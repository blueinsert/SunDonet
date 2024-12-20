﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using SunDonet.DB;
using SunDonet.Protocol;
using System.Diagnostics;
using log4net;
using System.IO;

namespace SunDonet
{
    public enum ServerState
    {
        None = -1,
        Starting,
        Running,
        Stoping,
        Stoped,
    }

    public class SunNet
    {
        protected static SunNet m_instance;
        public static SunNet Instance { get { return m_instance; } }
        public SunNet() { }

        public const string ConfigFilePath = "./Xml/Config.xml";

        public static SunNet CreateInstance()
        {
            m_instance = new SunNet();
            return m_instance;
        }

        public ServerState ServerState = ServerState.None;
        private ManualResetEvent m_exitEvent;

        private object m_connsLock = new object();
        public Dictionary<SocketIndentifier, Socket> m_id2SocketDic = new Dictionary<SocketIndentifier, Socket>();
        public Dictionary<Socket, SocketIndentifier> m_socket2IdDic = new Dictionary<Socket, SocketIndentifier>();
        public Dictionary<SocketIndentifier, Conn> m_connDic = new Dictionary<SocketIndentifier, Conn>();

        private int m_maxId = 1;
        private object m_servicesLock = new object();
        private Dictionary<int, ServiceBase> m_serviceDic = new Dictionary<int, ServiceBase>();
        private Dictionary<string, List<ServiceBase>> m_servicesByName = new Dictionary<string, List<ServiceBase>>();

        private object m_globalQueueLock = new object();
        /// <summary>
        /// 有消息要处理的服务
        /// </summary>
        private Queue<ServiceBase> m_globalServiceQueueWithWorkTodo = new Queue<ServiceBase>();

        private Semaphore m_workerSeamphore;
        /// <summary>
        /// 工作线程(task)
        /// </summary>
        private List<Worker> m_workers = new List<Worker>();
        public SocketWorker m_socketWorker = new SocketWorker();
        private BufferManager m_bufferManager;
        private ClassLoader m_classLoader;
        private AwaitableHandleManager m_awaitableHandleManager;

        private ThreadTimer m_timer;
        public ThreadTimer Timer { get { return m_timer; } }

        public MongoDBHelper DBHelper { get { return m_dbHelper; } }
        private MongoDBHelper m_dbHelper = null;

        public ProtocolDictionaryBase ProtocolDic { get { return m_protocolDic; } }
        protected ProtocolDictionaryBase m_protocolDic = null;

        private ServerConfig m_serverConfig = null;

        public ILog Log { get { return m_log; } }
        private ILog m_log = null;

        public ServerConfig GetServerConfig()
        {
            return m_serverConfig;
        }

        private bool InitializeConfig()
        {
            XmlLoader<ServerConfig> loader = new XmlLoader<ServerConfig>();
            if (!loader.Initialize(ConfigFilePath))
            {
                return false;
            }
            m_serverConfig = loader.Data;
            return true;
        }

        private bool IntializeLog()
        {
            var file = new FileInfo(m_serverConfig.Log.LogConfigPath);
            log4net.Config.XmlConfigurator.ConfigureAndWatch(file);

            // 获得日志接口
            m_log = LogManager.GetLogger(this.GetType().ToString());
            m_log.InfoFormat("SunNet::Initialization log started");
            return true;
        }

        private bool InitializeDB()
        {
            Debug.Log("SunNet:InitializeDB");
            var db = GetServerConfig().DBConfig;
            DB.MongoDBConfigInfo dbcfg = new DB.MongoDBConfigInfo()
            {
                DataBase = db.DataBase,
                ConnectHost = db.ConnectHost,
                Port = db.Port,
                UserName = db.UserName,
                Password = db.Password,
            };
            if (string.IsNullOrEmpty(dbcfg.DataBase)
                || string.IsNullOrEmpty(dbcfg.ConnectHost)
                || string.IsNullOrEmpty(dbcfg.Port))
            {
                Debug.Log("SunNet:InitializeDB ignore, not configed");
                return true;
            }
            try
            {
                m_dbHelper = new MongoDBHelper(dbcfg);
                Debug.Log("SunNet:InitializeDB success!");
                return true;
            }
            catch (Exception e)
            {
                Debug.Log("MongoDB connect failed:{0}", e.Message);
                return false;
            }
        }

        public bool Initialize()
        {
            ServerState = ServerState.Starting;
            Console.WriteLine("SunNet:Initialize");
            if (!InitializeConfig())
            {
                return false;
            }
            if (!IntializeLog())
            {
                SunNet.Instance.Log.Info("SunNet:Intialize IntializeLog Failed!");
                return false;
            }
            m_workerSeamphore = new Semaphore(0, 100);
            var networkCfg = GetServerConfig().NetworkConfig;
            var basicCfg = GetServerConfig().BasicConfig;
            m_bufferManager = new BufferManager(networkCfg.SocketInputBufferLen * 1000, networkCfg.SocketInputBufferLen);
            //m_globalQueueSemaphore = new SemaphoreSlim(0);
            m_bufferManager.InitBuffer();
            m_classLoader = ClassLoader.CreateClassLoader();
            Log.Info($"begin addAssembly");
            var mainDomainAssemblyList = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in mainDomainAssemblyList)
            {
                Log.Info($"addAssembly {assembly.GetName().Name}");
                m_classLoader.AddAssembly(assembly);
            }
            m_awaitableHandleManager = new AwaitableHandleManager();
            m_timer = new ThreadTimer(300);
            m_timer.AddTimer(AwaitableHandleManagerTimerCallBack, m_awaitableHandleManager, 0, 500);
            m_timer.StartTask();

            if (!InitializeDB())
            {
                return false;
            }

            InitClientProtocolDic();
            return true;
        }

        protected virtual void InitClientProtocolDic()
        {

        }


        private void StartSocketWorker()
        {
            m_socketWorker.Initialize();
            m_socketWorker.Start();
            SunNet.Instance.Log.Info("StartSocketWorker");
        }

        /// <summary>
        /// 开启若干个负载task(线程)，负责实际运行service
        /// </summary>
        private void StartWorkers()
        {
            int workerNum = GetServerConfig().BasicConfig.WorkerNum;
            for (int i = 0; i < workerNum; i++)
            {
                var worker = new Worker();
                worker.m_id = i;
                //延迟和效率统一
                worker.m_eachNum = 2 << i;
                worker.StartWorkerTask();
                SunNet.Instance.Log.Info("StartWorker id:" + i);
                m_workers.Add(worker);
            }
        }

        private void StopWorkers()
        {
            foreach (var woker in m_workers)
            {
                woker.Stop();
            }
            bool isAllStoped = false;
            while (!isAllStoped)
            {
                int waitingCount = 0;
                int nostopedCount = 0;
                foreach (var worker in m_workers)
                {
                    if (worker.State == WorkerState.Waiting)
                        waitingCount++;
                    if (worker.State != WorkerState.Stopped)
                        nostopedCount++;
                }
                isAllStoped = nostopedCount == 0;
                if (waitingCount != 0)
                {
                    CheckAndWeakUp();
                }
                Thread.Sleep(50);
            }
        }


        private void AwaitableHandleManagerTimerCallBack(Object obj)
        {
            m_awaitableHandleManager.Tick();
        }

        private ServiceConfig GetServiceConfigByName(string name)
        {
            foreach (var item in GetServerConfig().AllServiceList)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            Log.Error($"GetServiceConfigByName == null,name:{name}");

            return null;
        }

        /// <summary>
        /// 根据配置，加载初始默认的service
        /// </summary>
        private void StartInitServices()
        {
            var serviceList = GetServerConfig().InitServiceList;
            foreach (var item in serviceList)
            {
                var serviceCfg = GetServiceConfigByName(item.Name);

                if (serviceCfg != null)
                {
                    NewService(serviceCfg, null);
                }
            }

        }

        public void Start()
        {
            StartSocketWorker();
            StartWorkers();
            SunNet.Instance.Log.Info("Sunnet Start");
            ServerState = ServerState.Running;
            StartInitServices();
        }

        public void Stop()
        {
            OnStop();
        }

        protected virtual void OnStop()
        {

        }

        protected void DoExit()
        {
            m_exitEvent.Set();
        }

        private void DisposeSocket(SocketIndentifier id, Socket socket)
        {
            Debug.Log("Sunnet:DisposeSocket {0} {1}", id, socket);
            try
            {
                socket.Disconnect(false);
                socket.Close();
                socket.Dispose();
            }
            catch (SocketException ex)
            {
                Debug.Log("SocketErrorCode:{0}", ex.SocketErrorCode);
                if (id.SocketType == SocketType.Listen)
                {
                    if (ex.SocketErrorCode != SocketError.NotConnected)
                    {

                    }

                }
            }
            catch (Exception e)
            {
                Debug.Log("{0}", e);
            }
        }

        public void Uninitialize()
        {
            Debug.Log("SunNet:Uninitialize");
            //
            m_socketWorker.Stop();
            //
            StopWorkers();
            m_workers.Clear();
            m_workerSeamphore.Dispose();
            //
            List<int> allServiceIds = new List<int>();
            foreach (var pair in m_serviceDic)
            {
                allServiceIds.Add(pair.Key);
            }
            foreach (var id in allServiceIds)
            {
                KillService(id);
            }
            m_globalServiceQueueWithWorkTodo.Clear();
            //
            List<SocketIndentifier> socketIds = new List<SocketIndentifier>();
            foreach (var pair in m_connDic)
            {
                socketIds.Add(pair.Key);
            }
            foreach (var id in socketIds)
            {
                var conn = RemoveConn(id);
                var socket = conn.m_socket;
                DisposeSocket(id, socket);
            }
            Log.Logger.Repository.Shutdown(); //此函数调用之后日志无法打印了
        }

        public SocketIndentifier GetSocketId(Socket s)
        {
            return m_socket2IdDic[s];
        }

        public Socket GetSocket(SocketIndentifier id)
        {
            return m_id2SocketDic[id];
        }

        public Conn GetConn(SocketIndentifier id)
        {
            return m_connDic[id];
        }

        public Conn AddConn(SocketIndentifier id, Socket socket, int serviceId = -1)
        {
            m_id2SocketDic.Add(id, socket);
            m_socket2IdDic.Add(socket, id);
            var conn = new Conn(socket, id.SocketType, serviceId, m_bufferManager);
            lock (m_connsLock)
            {
                m_connDic.Add(id, conn);
            }
            return conn;
        }

        public Conn RemoveConn(SocketIndentifier id)
        {
            var socket = m_id2SocketDic[id];
            m_id2SocketDic.Remove(id);
            m_socket2IdDic.Remove(socket);
            Conn conn = null;
            lock (m_connsLock)
            {
                if (m_connDic.ContainsKey(id))
                {
                    conn = m_connDic[id];
                    m_connDic.Remove(id);
                }
            }
            return conn;
        }

        public void CloseConn(SocketIndentifier id, bool dispose = false)
        {
            var conn = RemoveConn(id);
            Debug.Log("Sunnet:CloseConn:{0}", conn);
            m_socketWorker.RemoveListenedConn(conn);
            var socket = conn.m_socket;
            if (dispose)
            {
                DisposeSocket(id, socket);
            }
        }

        public void Listen(int port, int serviceId = -1)
        {
            SunNet.Instance.Log.Info(string.Format("SunNet:Listen on {0},service:{1}", port, serviceId));
            var address = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(address, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
            if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
            }
            else
            {
                socket.Bind(localEndPoint);
            }
            socket.Listen(1024);
            SocketIndentifier id = new SocketIndentifier(string.Format("localhost:{0}", port));
            var conn = AddConn(id, socket, serviceId);
            m_socketWorker.AddListenedConn(conn);
        }

        public void Wait()
        {
            m_exitEvent = new System.Threading.ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                Stop();
                //m_exitEvent.Set();
            };

            m_exitEvent.WaitOne();
        }

        //"Test","lua-test"
        private int NewServiceImpl(string name, string className, Dictionary<string, string> paramDic = null)
        {
            if (className.StartsWith("lua-"))
            {
                //todo
            }
            else
            {
                var instance = m_classLoader.CreateInstance(new TypeDNName(className), 0);
                if (instance != null)
                {
                    var service = instance as ServiceBase;
                    if (service != null)
                    {
                        lock (m_servicesLock)
                        {
                            int id = m_maxId++;
                            service.m_id = id;
                            m_serviceDic.Add(id, service);
                            if (!m_servicesByName.ContainsKey(name))
                            {
                                m_servicesByName.Add(name, new List<ServiceBase>());
                            }
                            m_servicesByName[name].Add(service);
                        }
                        SunNet.Instance.Log.InfoFormat("SunNet:NewService {0} id:{1}", name, service.m_id);
                        service.SetParams(paramDic);
                        service.OnInit();
                        return service.m_id;
                    }
                }
            }
            return -1;
        }

        public int NewService(ServiceConfig serviceCfg, Dictionary<string, string> paramDic = null)
        {
            if (serviceCfg != null)
            {
                //todo merge dic
                if (!string.IsNullOrEmpty(serviceCfg.Params))
                {
                    var dic = ConfigureUtil.ParseParamDic(serviceCfg.Params);
                    return NewServiceImpl(serviceCfg.Name, serviceCfg.ClassName, dic);
                }
                else
                {
                    return NewServiceImpl(serviceCfg.Name, serviceCfg.ClassName);
                }
            }
            return -1;
        }

        public int NewService(string name, Dictionary<string, string> paramDic = null)
        {
            var serviceCfg = GetServiceConfigByName(name);
            return NewService(serviceCfg, paramDic);
        }

        public void KillService(int id)
        {
            Debug.Log("SunNet:KillService id:{0}", id);
            ServiceBase service = GetService(id);
            service.OnExit();
            lock (m_servicesLock)
            {
                m_serviceDic.Remove(id);
            }
        }

        protected ServiceBase GetService(int id)
        {
            ServiceBase service = null;
            lock (m_servicesLock)
            {
                service = m_serviceDic[id];
            }
            return service;
        }

        public int FindSingletonServiceByName(string name)
        {
            if (m_servicesByName.ContainsKey(name))
            {
                if (m_servicesByName[name].Count > 1)
                {
                    //debug.error
                }
                if (m_servicesByName[name].Count == 1)
                {
                    return m_servicesByName[name][0].m_id;
                }
            }

            SunNet.Instance.Log.Info(string.Format("FindSingletonServiceByName failed! name:{0}", name));
            return -1;
        }

        /// <summary>
        /// 发送给本地服务
        /// </summary>
        /// <param name="to"></param>
        /// <param name="msg"></param>
        public void SendInternal(int to, MsgBase msg)
        {
            var service = GetService(to);
            if (service != null)
            {
                service.PushMsg(msg);
                lock (service.m_isInGlobalLock)
                {
                    if (!service.m_isInGlobal)
                    {
                        PushServiceWithWorkTodo(service);
                        service.m_isInGlobal = true;
                    }
                }
            }
        }

        /// <summary>
        /// 向某个服务发生ntf消息
        /// </summary>
        /// <param name="to"></param>
        /// <param name="ntf"></param>
        public void Send(int to, ServiceMsgNtf ntf)
        {
            SendInternal(to, ntf);
        }

        public void SetAck(int to, ServiceMsgAck ack)
        {
            var token = ack.m_token;
            m_awaitableHandleManager.SetResult(token, ack);
        }

        public async Task<TAck> Call<TReq, TAck>(int to, TReq req) where TReq : ServiceMsgReq where TAck : ServiceMsgAck
        {
            var handle = m_awaitableHandleManager.AllocHandle(60 * 10);
            req.m_token = handle.Token;
            SendInternal(to, req);
            await handle.WaitAsync();
            if (!handle.IsTimeOut && !handle.IsCancel)
            {
                return handle.GetResult<TAck>();
            }
            return null;
        }

        /// <summary>
        /// 发送给客户端,同步方法
        /// </summary>
        /// <param name="s"></param>
        /// <param name="buff"></param>
        public void SendPackage(SocketIndentifier sid, ClientBuffer buff)
        {
            var conn = GetConn(sid);
            conn?.SendPackage(buff); 
        }

        /// <summary>
        /// 将一个有任务的service进队列
        /// </summary>
        /// <param name="service"></param>
        public void PushServiceWithWorkTodo(ServiceBase service)
        {
            lock (m_globalQueueLock)
            {
                m_globalServiceQueueWithWorkTodo.Enqueue(service);
            }
            CheckAndWeakUp();
        }

        /// <summary>
        /// 将一个有任务的service出队列
        /// </summary>
        /// <returns></returns>
        public ServiceBase PopServiceWithWorkTodo()
        {
            ServiceBase service = null;
            lock (m_globalQueueLock)
            {
                if (m_globalServiceQueueWithWorkTodo.Count != 0)
                {
                    service = m_globalServiceQueueWithWorkTodo.Dequeue();
                }
            }
            return service;
        }

        //Worker线程调用，进入休眠
        public void WorkerWait()
        {
            //Thread.Sleep(100);//todo
            m_workerSeamphore.WaitOne();
        }

        public void CheckAndWeakUp()
        {
            m_workerSeamphore.Release();
        }
    }

    /// <summary>
    /// 可等待句柄
    /// </summary>
    public class AwaitableHandle
    {
        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="token"></param>
        /// <param name="timeoutSeconds"></param>
        public AwaitableHandle(Int32 token, int timeoutSeconds = 10)
        {
            Token = token;
            TimeoutTime = timeoutSeconds > 0 ? DateTime.Now.AddSeconds(timeoutSeconds) : DateTime.MaxValue;
            IsTimeOut = false;
        }

        /// <summary>
        /// 发起等待
        /// </summary>
        /// <returns></returns>
        public async Task WaitAsync()
        {
            await m_tcs.Task;
        }

        /// <summary>
        /// 设置结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        public void SetResult<T>(T result)
            where T : class
        {
            m_result = result;
            //SetResult默认会使用调用线程执行 await 后面的代码
            //所以需要提供一个 Task.Run让线程池线程执行await 后面的代码
            //m_tcs.SetResult(true);
            Task.Run(() => { m_tcs.SetResult(true); });

        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetResult<T>()
            where T : class
        {
            return m_result as T;
        }

        /// <summary>
        /// 设置超时
        /// </summary>
        public void SetTimeout()
        {
            IsTimeOut = true;
            //m_tcs.SetResult(true);
            Task.Run(() => { m_tcs.SetResult(true); });
        }

        /// <summary>
        /// 设置取消
        /// </summary>
        public void SetCancel()
        {
            IsCancel = true;
            //m_tcs.SetResult(true);
            Task.Run(() => { m_tcs.SetResult(true); });
        }

        /// <summary>
        /// 用来标识一次rpccall的现场
        /// </summary>
        public Int32 Token { get; private set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public DateTime TimeoutTime { get; private set; }

        /// <summary>
        /// 是否已经超时
        /// </summary>
        public bool IsTimeOut { get; private set; }

        /// <summary>
        /// 是否取消
        /// </summary>
        public bool IsCancel { get; private set; }

        /// <summary>
        /// Task await 支持
        /// </summary>
        private TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();

        /// <summary>
        /// 用来保存等待后得到的结果数据
        /// </summary>
        private Object m_result;
    }

    /// <summary>
    /// AwaitableHandle 的管理器
    /// </summary>
    public class AwaitableHandleManager
    {
        /// <summary>
        /// 分配一个AwaitableHandle
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public AwaitableHandle AllocHandle(int timeoutSeconds)
        {
            Int32 token = Interlocked.Increment(ref m_tokenSeed);
            AwaitableHandle handle = new AwaitableHandle(token, timeoutSeconds);
            m_token2AwaitableHandleDict[token] = handle;
            return handle;
        }

        /// <summary>
        /// 设置取消
        /// </summary>
        /// <param name="token"></param>
        public bool Cancel(int token)
        {
            AwaitableHandle handle = null;
            if (m_token2AwaitableHandleDict.TryRemove(token, out handle))
            {
                SunNet.Instance.Log.InfoFormat("AwaitableHandleManager handle Cancel token={0}", token);

                handle.SetCancel();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置完成结果
        /// </summary>
        /// <param name="token"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool SetResult(int token, Object result)
        {
            AwaitableHandle handle = null;
            if (m_token2AwaitableHandleDict.TryRemove(token, out handle))
            {
                handle.SetResult(result);
                return true;
            }
            return false;
        }

        /// <summary>
        /// tick处理超时
        /// </summary>
        public void Tick()
        {
            DateTime currTime = DateTime.Now;
            //SunNet.Instance.Log.Info(string.Format("AwaitableHandleManager tick:{0}", currTime));

            foreach (var item in m_token2AwaitableHandleDict)
            {
                if (item.Value.TimeoutTime <= currTime)
                {
                    m_tobeRemove.Add(item.Key);
                }
            }
            foreach (var token in m_tobeRemove)
            {
                AwaitableHandle handle;
                if (m_token2AwaitableHandleDict.TryRemove(token, out handle))
                {
                    SunNet.Instance.Log.InfoFormat(
                        "AwaitableHandleManager handle time out token={0}", token);

                    handle.SetTimeout();
                }
            }
            m_tobeRemove.Clear();
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            foreach (var item in m_token2AwaitableHandleDict)
            {
                item.Value.SetTimeout();
            }
            m_token2AwaitableHandleDict.Clear();
        }

        /// <summary>
        /// token和AwaitableHandle的关联字典
        /// </summary>
        private ConcurrentDictionary<Int32, AwaitableHandle> m_token2AwaitableHandleDict = new ConcurrentDictionary<int, AwaitableHandle>();

        /// <summary>
        /// 需要移除的token
        /// </summary>
        private List<Int32> m_tobeRemove = new List<int>();

        /// <summary>
        /// token种子
        /// </summary>
        private Int32 m_tokenSeed = 0;
    }
}
