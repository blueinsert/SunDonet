using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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

        private object m_globalQueueLock = new object();
        private Queue<ServiceBase> m_globalServiceQueue = new Queue<ServiceBase>();//有消息要处理的服务
        

        private List<Worker> m_workers = new List<Worker>();
        public SocketWorker m_socketWorker = new SocketWorker();
        private BufferManager m_bufferManager;
        private ClassLoader m_classLoader;

        private void StartSocketWorker()
        {
            m_socketWorker.Init();
            m_socketWorker.Start();
            Console.WriteLine("StartSocketWorker");
        }

        private void StartWorker()
        {
            for(int i = 0; i < 2; i++)
            {
                var worker = new Worker();
                worker.m_id = i;
                worker.m_eachNum = 2 << i;
                worker.StartWorkerTask();
                Console.WriteLine("StartWorker id:" + i);
                m_workers.Add(worker);
            }
        }

        public void Init()
        {
            m_bufferManager = new BufferManager(1024 * 128, 1024);
            //m_globalQueueSemaphore = new SemaphoreSlim(0);
            m_bufferManager.InitBuffer();
            m_classLoader = ClassLoader.CreateClassLoader();
            var mainDomainAssemblyList = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in mainDomainAssemblyList)
            {
                m_classLoader.AddAssembly(assembly);
            }
        }

        public void Start()
        {
            StartSocketWorker();
            StartWorker();
            Console.WriteLine("Sunnet Start");
        }

        public Conn AddConn(Socket socket,SocketType type,int serviceId = -1)
        {
            var conn = new Conn(socket, type, serviceId, m_bufferManager);
            lock (m_connsLock) { 
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

        public void Listen(int port,int serviceId = -1)
        {
            Console.WriteLine(string.Format("SunNet:Listen on {0},service:{1}", port, serviceId));
            var address = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(address, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
            if(localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
            }
            else
            {
                socket.Bind(localEndPoint);
            }
            socket.Listen(1024);
            var conn = AddConn(socket, SocketType.Listen,serviceId);
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
            Console.WriteLine("SunNet:NewService " + name);
            if (name.StartsWith("lua-"))
            {
                //todo
            }
            else
            {
                var instance = m_classLoader.CreateInstance(new TypeDNName(string.Format("SunDonet@SunDonet.{0}", name)),0);
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
                        }
                        service.OnInit();
                        return service.m_id;
                    }
                }
            }
            return -1;
        }

        public void KillService(int id) {

        }

        public ServiceBase GetService(int id)
        {
            ServiceBase service = null;
            lock (m_servicesLock)
            {
                service = m_serviceDic[id];
            }
            return service;
        }

        public void Send(int to, MsgBase msg)
        {
            var service = GetService(to);
            if (service != null)
            {
                service.PushMsg(msg);
                if (!service.m_isInGlobal)
                {
                    lock (m_globalQueueLock)
                    {
                        PushGlobalQueue(service);
                        service.m_isInGlobal = true;
                    }
                }
            }
        }

        public void PushGlobalQueue(ServiceBase service)
        {
            lock (m_globalQueueLock)
            {
                m_globalServiceQueue.Enqueue(service);
            }
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
            Thread.Sleep(100);//todo
        }
    }

    // This class creates a single large buffer which can be divided up
    // and assigned to SocketAsyncEventArgs objects for use with each
    // socket I/O operation.
    // This enables bufffers to be easily reused and guards against
    // fragmenting heap memory.
    //
    // The operations exposed on the BufferManager class are not thread safe.
    public class BufferManager
    {
        int m_numBytes;                 // the total number of bytes controlled by the buffer pool
        byte[] m_buffer;                // the underlying byte array maintained by the Buffer Manager
        Stack<int> m_freeIndexPool;     //
        int m_currentIndex;
        int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        // Allocates buffer space used by the buffer pool
        public void InitBuffer()
        {
            // create one big large buffer and divide that
            // out to each SocketAsyncEventArg object
            m_buffer = new byte[m_numBytes];
        }

        // Assigns a buffer from the buffer pool to the
        // specified SocketAsyncEventArgs object
        //
        // <returns>true if the buffer was successfully set, else false</returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {

            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.
        // This frees the buffer back to the buffer pool
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
