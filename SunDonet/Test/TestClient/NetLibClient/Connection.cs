using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using SunDonet.Protocol;
using SunDonet;
using Google.Protobuf;
using Debug =  System.Console;

namespace NetLibClient
{

    public enum ConnectionState
    {
        /// <summary>
        ///     Idle状态
        /// </summary>
        None = 0,

        /// <summary>
        ///     连接中
        /// </summary>
        Connecting = 1,

        /// <summary>
        ///     已连接
        /// </summary>
        Established = 2,

        /// <summary>
        ///     断开中
        /// </summary>
        Disconnecting = 3,

        /// <summary>
        ///     已关闭
        /// </summary>
        Closed = 4
    }

    [Synchronization]
    public class Connection
    {
        public const int MaxPackageLength = 1024 * 64;

        public Connection(IProtoProvider provider)
        {
            State = ConnectionState.None;
            m_provider = provider;
        }

        public bool Initialize(string remoteAddress, int remotePort)
        {
            // 客户端处理IPV6的时候逻辑，需要用到
#if UNITY_5_3_OR_NEWER
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                String newServerIp = "";
                AddressFamily newAddressFamily = AddressFamily.InterNetwork;
                GetIpType(remoteAddress, remotePort.ToString(), out newServerIp, out newAddressFamily);
                if (!string.IsNullOrEmpty(newServerIp)) { remoteAddress = newServerIp; }
            }
#endif

            // analyze the ip address and port info
            IPAddress ipAddress;
            if (IPAddress.TryParse(remoteAddress, out ipAddress))
                m_ipEndPoint = new IPEndPoint(ipAddress, remotePort);
            else
                return false;

            // Do some cleanup if necessery
            if (State != ConnectionState.None)
                Close();

            // Clean all messages of last connection
            m_recvQueue.Clear();

            // Clean all event args of last connection

            // 启动连接
            StartConnection();

            return true;
        }

        /// <summary>
        /// 打开链接
        /// </summary>
        private void StartConnection()
        {
            // Change state
            State = ConnectionState.Connecting;

            m_socket = new Socket(m_ipEndPoint.AddressFamily, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);

            // 构造异步连接参数
            m_connEventArg = new SocketAsyncEventArgs();
            m_connEventArg.Completed += OnCompletedForConnect;
            m_connEventArg.RemoteEndPoint = m_ipEndPoint;

            // 发起异步连接
            if (!m_socket.ConnectAsync(m_connEventArg))
            {
                // 当还没开始异步过程就是败了
                OnCompletedForConnect(null, m_connEventArg);
            }
        }

        /// <summary>
        /// 主动断开连接
        /// </summary>
        public void Disconnect()
        {
            if (State != ConnectionState.Established)
            {
                return;
            }

            var oldSocket = m_socket;

            // 启动断开连接的timer
            if (m_disconnectTimer == null)
            {
                m_disconnectTimer = new Timer(
                    (o) =>
                    {
                        // 如果还没有对上层进行驱动，手动通知上层，连接断开
                        if (oldSocket == m_socket &&
                            !m_socket.Connected &&
                            State == ConnectionState.Disconnecting)
                        {
                            // 记录日志
                            //Debug.WriteLine(string.Format("SocketDisconnect disconnected timer start..."));

                            FireEventOnLogPrint("Disconnect.Timer" + "state=" + State);

                            // 设置状态
                            State = ConnectionState.Closed;
                        }
                    }, null, 500, 1000); // 500毫秒后启动， 1000毫秒检查一次

                try
                {
                    m_socket.Shutdown(SocketShutdown.Both);
                    //m_socket.Disconnect(false);
                }
                catch (Exception ex)
                {
                    string msg = string.Format("{0}", ex);
                    Debug.WriteLine(msg);
                }
            }

            State = ConnectionState.Disconnecting;
        }

        /// <summary>
        /// 关闭连接，初始化状态
        /// </summary>
        public void Close()
        {
            try
            {
                if (m_socket != null)
                {
                    m_socket.Close();
                }

                // 删除两个IDispose数据，避免connect释放不了的问题
                if (m_connEventArg != null)
                {
                    m_connEventArg.Dispose();
                    m_connEventArg = null;
                }

                if (m_receiveEventArg != null)
                {
                    m_receiveEventArg.Dispose();
                    m_receiveEventArg = null;
                }

                // 清理timer
                if (m_disconnectTimer != null)
                {
                    m_disconnectTimer.Dispose();
                    m_disconnectTimer = null;
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}", ex.GetType(), ex.Message);
                Debug.WriteLine(msg);
                //throw ex;
            }

            State = ConnectionState.None;
        }

        /// <summary>
        ///     Get underline protocol message.
        /// </summary>
        /// <returns>If exist message return it ,or reutrn null.</returns>
        public KeyValuePair<int, object> GetMessagePair()
        {
            // 尝试将缓存中的数据解包
            TryDecodeMsg();
            KeyValuePair<int, object> msgPair = new KeyValuePair<int, object>(0, null);
            lock (m_recvQueue)
            {
                if (m_recvQueue.Count > 0)
                {
                    msgPair = m_recvQueue.Dequeue();
                    //return msgPair;
                }
            }
            return msgPair;
        }

        public void SendMessage(System.Object msg)
        {
            if (State != ConnectionState.Established)
            {
                Debug.WriteLine(string.Format("SendMessage Error:in State {0}", State));
                return;
            }
            if (msg == null)
            {
                Debug.WriteLine("SendMessage Error:msg is null");
                return;
            }

            var sendBuf = GoogleProtobufHelper.EncodeMessage(msg as IMessage, m_provider);

            var sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.Completed += OnCompletedForSend;
            sendEventArgs.SetBuffer(sendBuf.m_buffer, 0, sendBuf.m_dataLen);
            ClientBuffer.BackBuffer(sendBuf);

            Debug.WriteLine(string.Format("SendMessage Send {0}", msg.GetType().Name));

            if (!m_socket.SendAsync(sendEventArgs))
            {
                OnCompletedForSendImpl(sendEventArgs);
                sendEventArgs.Dispose();
            }
        }

        private void OnCompletedForSend(System.Object sender, SocketAsyncEventArgs e)
        {
            OnCompletedForSendImpl(e);
            e.Dispose();
        }

        //The callback of BeginSend on fuction "SendMessage"
        private void OnCompletedForSendImpl(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                    return;
                throw new Exception("The result of SendAsync is not correct");
            }
            catch (Exception ex)
            {
                // Connection exception means connection is disconnected

            }

            if (State == ConnectionState.Established)
            {
                State = ConnectionState.Closed;
                FireEventOnLogPrint("OnCompletedForSendImpl");
            }

        }

        /// <summary>
        /// the callback when socket ConnectAsync finished
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnCompletedForConnect(System.Object sender, SocketAsyncEventArgs eventArgs)
        {
            try
            {
                // 如果连接失败
                if (eventArgs.SocketError != SocketError.Success)
                {
                    State = ConnectionState.Closed;
                    return;
                }

                // 连接成功
                State = ConnectionState.Established;

                // 初始化接收参数
                m_receiveEventArg = new SocketAsyncEventArgs();
                m_receiveEventArg.Completed += OnCompletedForReceive;
                m_receiveEventArg.SetBuffer(new byte[MaxPackageLength], 0, MaxPackageLength);

                // 发起接收
                if (!m_socket.ReceiveAsync(m_receiveEventArg))
                {
                    // 接收失败
                    OnCompletedForReceive(null, m_receiveEventArg);
                }
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// 当接收数据操作完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnCompletedForReceive(System.Object sender, SocketAsyncEventArgs eventArgs)
        {
            try
            {
                // 错误处理
                if (eventArgs.SocketError != SocketError.Success)
                {
                    throw new Exception(string.Format("The result of ReceiveAsync is not correct, SocketError={0}", eventArgs.SocketError));
                }
                if (eventArgs.BytesTransferred == 0)
                {
                    goto LB_CONNBREAK;
                }

                // 缓存数据
                lock (m_recvCache)
                {
                    m_recvCache.Write(eventArgs.Buffer, eventArgs.Offset, eventArgs.BytesTransferred);
                }


                //Debug.WriteLine(string.Format("OnCompletedForReceiveImpl Receive {0} RecvCache.Length={1}", eventArgs.BytesTransferred, RecvCache.Length));

                // 重新发起接收
                m_receiveEventArg.SetBuffer(0, MaxPackageLength);
                if (!m_socket.ReceiveAsync(m_receiveEventArg))
                {
                    OnCompletedForReceive(null, m_receiveEventArg);
                }
                return;
            }
            catch (Exception ex)
            {
                // Connection exception means connection is disconnected
                // Or, there is an bad message format in byte stream
            }

            LB_CONNBREAK:
            // when receive action is over, which represents that socket can't receive any data
            if (State == ConnectionState.Established ||
                State == ConnectionState.Disconnecting) // 这个状态是为了让客户端主动断开服务器的时候
            {
                FireEventOnLogPrint("OnCompletedForReceive" + "state=" + State);
                State = ConnectionState.Closed;
            }
        }

        /// <summary>
        /// 尝试将缓存中的数据解包
        /// </summary>
        private void TryDecodeMsg()
        {
            lock (m_recvCache)
            {

                if (m_lastRecvCacheLength == m_recvCache.Length)
                {
                    return;
                }

                // second, push the msg to recvqueue and decode message
                while (true)
                {
                    // 将数据解包
                    object msgObj;
                    m_recvCache.Crunch();
                    int byteHandled = GoogleProtobufHelper.DecodeMessage(m_recvCache.m_buffer, m_recvCache.Length, m_provider, out msgObj);
                    if (msgObj == null)
                        break;
                    m_recvCache.ReadSkip(byteHandled);
                    int msgId = m_provider.GetIdByType(msgObj.GetType());
                    var msgPair = new KeyValuePair<int, object>(msgId, msgObj);
                    lock (m_recvQueue)
                    {
                        m_recvQueue.Enqueue(msgPair);
                    }
                    //m_recvCache.Crunch();
                    // all cache is handled
                    if (m_recvCache.Length == 0)
                        break;
                }

                //Debug.WriteLine(string.Format("OnCompletedForReceiveImpl Receive End, RecvCache.Length={0}", RecvCache.Length));
                
                m_lastRecvCacheLength = m_recvCache.Length;
            }
        }

        #region IPV6

#if UNITY_5_3_OR_NEWER && UNITY_IOS
        [DllImport("__Internal")]
        private static extern string getIPv6(string mHost, string mPort);
#endif
        /// <summary>
        /// 获取服务器ip对应的ipv6的地址表示字符串，默认为"192.168.1.1&& ipv4"
        /// </summary>
        /// <param name="mHost"></param>
        /// <param name="mPort"></param>
        /// <returns></returns>
        public static string GetIPv6String(string mHost, string mPort)
        {
#if UNITY_5_3_OR_NEWER && UNITY_IOS
                string mIPv6 = getIPv6(mHost, mPort);
                return mIPv6;
#endif
            return mHost + "&&ipv4";
        }

        /// <summary>
        ///     将服务器ip转换为ipv6网络中的新地址
        /// </summary>
        /// <param name="serverIp"></param>
        /// <param name="serverPorts"></param>
        /// <param name="newServerIp"></param>
        /// <param name="mIpType"></param>
        private void GetIpType(string serverIp, string serverPorts, out string newServerIp, out AddressFamily mIpType)
        {
            mIpType = AddressFamily.InterNetwork;
            newServerIp = serverIp;
            try
            {
                string mIPv6 = GetIPv6String(serverIp, serverPorts);
                if (!string.IsNullOrEmpty(mIPv6))
                {
                    string[] strTemp = System.Text.RegularExpressions.Regex.Split(mIPv6, "&&");
                    if (strTemp != null && strTemp.Length >= 2)
                    {
                        string ipType = strTemp[1];
                        if (ipType == "ipv6")
                        {
                            newServerIp = strTemp[0];
                            mIpType = AddressFamily.InterNetworkV6;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        private void FireEventOnLogPrint(string callFun, string log = "")
        {
            if (string.IsNullOrEmpty(log))
            {
                log = string.Format("m_ipEndPoint={0}:{1} CCMSGConnectionBreak happen. callFun={2}",
                    m_ipEndPoint.Address.ToString(), m_ipEndPoint.Port, callFun);
            }

            if (EventOnLogPrint != null)
            {
                EventOnLogPrint(log);
            }
        }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int GetEndpoint()
        {
            if (m_ipEndPoint == null)
            {
                return 0;
            }

            return m_ipEndPoint.Port;
        }

        /// <summary>
        ///  The connection state store and management
        /// </summary>
        public ConnectionState State { get; private set; }


        /// <summary>
        /// Underline protocol provider
        /// </summary>
        private IProtoProvider m_provider = null;

        /// <summary>
        ///     Underline socket intance
        /// </summary>
        private Socket m_socket;

        /// <summary>
        /// Local byte cahce for recv operation
        /// </summary>
        private MessageBlock m_recvCache = new MessageBlock(MaxPackageLength);

        /// <summary>
        /// The protocol message to be processed
        /// </summary>
        private Queue<KeyValuePair<int, System.Object>> m_recvQueue = new Queue<KeyValuePair<int, System.Object>>();

        /// <summary>
        /// 上一次解包处理的时候m_recvCache的长度
        /// </summary>
        private int m_lastRecvCacheLength;

        /// <summary>
        /// remote address
        /// </summary>
        private IPEndPoint m_ipEndPoint;

        /// <summary>
        /// The eventArg for socket connect action
        /// </summary>
        private SocketAsyncEventArgs m_connEventArg = new SocketAsyncEventArgs();

        /// <summary>
        /// The eventArg for socket receive action
        /// </summary>
        private SocketAsyncEventArgs m_receiveEventArg = new SocketAsyncEventArgs();

        /// <summary>
        /// sokect断开后的timer，用来处理可能收不到后续Fin包的情况
        /// </summary>
        private Timer m_disconnectTimer;

        public event Action<string> EventOnLogPrint;
    }

   
}
