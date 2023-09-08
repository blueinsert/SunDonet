using System;
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

namespace SunDonet
{
    public class SunNet
    {
        private static SunNet m_instance;
        public static SunNet Instance { get { return m_instance; } }
        private SunNet() { Init(); }
        public static SunNet CreateInstance()
        {
            m_instance = new SunNet();
            return m_instance;
        }

        private object m_connsLock = new object();
        public Dictionary<Socket, Conn> m_connDic = new Dictionary<Socket, Conn>();

        private int m_maxId = 1;
        private object m_servicesLock = new object();
        private Dictionary<int, ServiceBase> m_serviceDic = new Dictionary<int, ServiceBase>();
        private Dictionary<string, List<ServiceBase>> m_servicesByName = new Dictionary<string, List<ServiceBase>>();

        private object m_globalQueueLock = new object();
        private Queue<ServiceBase> m_globalServiceQueue = new Queue<ServiceBase>();//有消息要处理的服务

        private Semaphore m_workerSeamphore;
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
        private ProtocolDictionaryBase m_protocolDic = null;

        private void StartSocketWorker()
        {
            m_socketWorker.Init();
            m_socketWorker.Start();
            Console.WriteLine("StartSocketWorker");
        }

        private void StartWorker()
        {
            for (int i = 0; i < 5; i++)
            {
                var worker = new Worker();
                worker.m_id = i;
                //延迟和效率统一
                worker.m_eachNum = 2 << i;
                worker.StartWorkerTask();
                Console.WriteLine("StartWorker id:" + i);
                m_workers.Add(worker);
            }
        }

        public void Init()
        {
            m_workerSeamphore = new Semaphore(0, 100);
            m_bufferManager = new BufferManager(8 * 1024 * 5 * 1024, 8 * 1024 * 5);
            //m_globalQueueSemaphore = new SemaphoreSlim(0);
            m_bufferManager.InitBuffer();
            m_classLoader = ClassLoader.CreateClassLoader();
            var mainDomainAssemblyList = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in mainDomainAssemblyList)
            {
                m_classLoader.AddAssembly(assembly);
            }
            m_awaitableHandleManager = new AwaitableHandleManager();
            m_timer = new ThreadTimer(300);
            m_timer.AddTimer(AwaitableHandleManagerTimerCallBack, m_awaitableHandleManager, 0, 500);
            m_timer.StartTask();

            MongoDBConfigInfo dbcfg = new MongoDBConfigInfo()
            {
                DataBase = "MyDB",
                ConnectHost = "127.0.0.1",
                Port = "27017",
                //UserName = "bluebean",
                //Password = "1234",
            };
            try
            {
                m_dbHelper = new MongoDBHelper(dbcfg);
            }
            catch (Exception e)
            {
                Console.WriteLine("MongoDB connect failed");
                throw e;
            }

            m_protocolDic = new SunDonetProtocolDictionary();
        }

        private void AwaitableHandleManagerTimerCallBack(Object obj)
        {
            m_awaitableHandleManager.Tick();
        }

        public void Start()
        {
            StartSocketWorker();
            StartWorker();
            Console.WriteLine("Sunnet Start");
        }

        public Conn AddConn(Socket socket, SocketType type, int serviceId = -1)
        {
            var conn = new Conn(socket, type, serviceId, m_bufferManager);
            lock (m_connsLock)
            {
                m_connDic.Add(socket, conn);
            }
            return conn;
        }

        public Conn RemoveConn(Socket socket)
        {
            Conn conn = m_connDic[socket];
            lock (m_connsLock)
            {
                m_connDic.Remove(socket);
            }
            return conn;
        }

        public void CloseConn(Socket socket)
        {
            var conn = RemoveConn(socket);
            m_socketWorker.RemoveEvent(conn);
        }

        public void Listen(int port, int serviceId = -1)
        {
            Console.WriteLine(string.Format("SunNet:Listen on {0},service:{1}", port, serviceId));
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
            var conn = AddConn(socket, SocketType.Listen, serviceId);
            m_socketWorker.AddEvent(conn);
        }

        public void Wait()
        {
            var exitEvent = new System.Threading.ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();
        }

        //"Test","lua-test"
        public int NewService(string name)
        {
            if (name.StartsWith("lua-"))
            {
                //todo
            }
            else
            {
                var instance = m_classLoader.CreateInstance(new TypeDNName(string.Format("SunDonet@SunDonet.{0}", name)), 0);
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
                        Console.WriteLine("SunNet:NewService {0} id:{1}", name, service.m_id);
                        service.OnInit();
                        return service.m_id;
                    }
                }
            }
            return -1;
        }

        public void KillService(int id)
        {

        }

        private ServiceBase GetService(int id)
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
                        PushGlobalQueue(service);
                        service.m_isInGlobal = true;
                    }
                }
            }
        }

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
        /// 发送给客户端
        /// </summary>
        /// <param name="s"></param>
        /// <param name="buff"></param>
        public void Send(Socket s, ClientBuffer buff)
        {
            //todo
            s.Send(buff.m_buffer, buff.m_dataLen, SocketFlags.None);
            ClientBuffer.BackBuffer(buff);
        }

        public void PushGlobalQueue(ServiceBase service)
        {
            lock (m_globalQueueLock)
            {
                m_globalServiceQueue.Enqueue(service);
            }
            CheckAndWeakUp();
        }

        public ServiceBase PopGlobalQueue()
        {
            ServiceBase service = null;
            lock (m_globalQueueLock)
            {
                if (m_globalServiceQueue.Count != 0)
                {
                    service = m_globalServiceQueue.Dequeue();
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
                Console.WriteLine("AwaitableHandleManager handle Cancel token={0}", token);

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
            //Console.WriteLine(string.Format("AwaitableHandleManager tick:{0}", currTime));

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
                    Console.WriteLine(
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
