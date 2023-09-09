using SunDonet.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLibClient
{
    public class Client
    {
        /// <summary>
        /// 网路层连接句柄
        /// </summary>
        private Connection m_connect;

        /// <summary>
        /// 协议字典
        /// </summary>
        private IProtoProvider m_protoProvider;

        /// <summary>
        /// 是否阻塞协议处理的Loop
        /// </summary>
        private bool m_isBlockProcessMsg;

        /// <summary>
        /// 建立连接的地址
        /// </summary>
        private string m_connectedAddress;

        private IClientEventHandler m_clientEventHandler;

        public Client(IClientEventHandler eventHandler,IProtoProvider protoProvider)
        {
            m_clientEventHandler = eventHandler;
            m_protoProvider = protoProvider;
            m_connect = new Connection(m_protoProvider);
        }

        /// <summary>
        /// 设置协议处理loop的阻塞状态
        /// 如果阻塞住这个loop，则客户端接受到的网络消息会一直累积在消息队列里，直到这个阻塞被释放
        /// </summary>
        /// <param name="isBlock"></param>
        public void BlockProcessMsg(bool isBlock)
        {
            m_isBlockProcessMsg = isBlock;
        }

        /// <summary>
        /// 主动关闭连接
        /// </summary>
        /// <returns>关闭命令发送成功：true</returns>
        public bool Disconnect()
        {
            if (m_connect.State == ConnectionState.None || m_connect.State == ConnectionState.Closed)
                return true;
            m_connect.Disconnect();
            return true;
        }

        /// <summary>
        /// 结束socket
        /// </summary>
        public void Close()
        {
            m_connect.Close();
        }

        /// <summary>
        /// 发包
        /// </summary>
        /// <param name="msg">包</param>
        /// <returns>发送成功：true</returns>
        public bool SendMessage(object msg)
        {
            if (m_connect.State != ConnectionState.Established)
            {
                //Debug.WriteLine("Client::SendMessage, checked _connect.State failed");
                m_clientEventHandler.OnError((int)ClientErrId.ConnectionSendFailure, "Client::SendMessage state not equal to established");
                return false;
            }
            Console.WriteLine(string.Format("Client:SendMessage {0} {1}",msg.GetType(), msg));
            m_connect.SendMessage(msg);
            return true;
        }

        /// <summary>
        /// Tick函数由上层逻辑循环调用
        /// </summary>
        public void Tick()
        {
            ProcessMessages();
        }

        /// <summary>
        /// 消息处理
        /// </summary>
        /// <param name="proc"></param>
        private void ProcessMessages()
        {
            KeyValuePair<int, Object> msgPair;
            Int32 msgId;

            while (!m_isBlockProcessMsg)
            {
                msgPair = m_connect.GetMessagePair();
                if (msgPair.Value == null)
                {
                    break;
                }
                msgId = msgPair.Key;
                m_clientEventHandler.OnMessage(msgPair.Value, msgId);
            }
        }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int GetEndPoint()
        {
            if (m_connect == null)
            {
                return 0;
            }

            return m_connect.GetEndpoint();
        }

        public void RegConnectionLogEvent(Action<string> logEvent)
        {
            m_connect.EventOnLogPrint += logEvent;
        }

        public string GetIp()
        {
            return m_connectedAddress;
        }

        public bool Connect(string serverAddress, int serverPort)
        {
            //if (!m_connect.Initialize(serverAddress, serverPort, serverDomain))
            if (!m_connect.Initialize(serverAddress, serverPort))
            {
                //Debug.WriteLine("Client::LoginByAuthToken, call _connect.Initialize failed");
                return false;
            }
            m_connectedAddress = serverAddress;
            return true;
        }

    }
}
