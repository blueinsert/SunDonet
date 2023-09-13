using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SunDonet
{
    public class SocketIndentifier
    {
        public string Remote = "";
        public SocketType SocketType;
        public string ListenAddress = "";

        public SocketIndentifier(Socket s)
        {
            this.SocketType = SocketType.Normal;
            this.Remote = s.RemoteEndPoint.ToString();
        }

        public SocketIndentifier(string listenAddress)
        {
            this.SocketType = SocketType.Listen;
            this.ListenAddress = listenAddress;
        }

        public override string ToString()
        {
            if (SocketType == SocketType.Listen)
            {
                return string.Format("{0} {1}", SocketType, ListenAddress);
            }
            else
            {
                return string.Format("{0} {1}", SocketType, Remote);
            }
        }
    }

    public enum SocketType
    {
        Normal,
        Listen,
    }

    public class Conn
    {
        public int m_serviceId;

        public SocketType m_socketType;
        public Socket m_socket;
        public SocketAsyncEventArgs m_event;

        public byte[] receiveBuff = new byte[1024 * 5];
        public int m_receiveLen = 0;

        public Task<Conn> m_task;
        private ManualResetEvent m_taskCompleteEvent;

        public Conn(Socket socket, SocketType type, int serviceId = -1, BufferManager bufferManager = null)
        {
            m_serviceId = serviceId;
            m_socketType = type;
            m_socket = socket;
            m_event = new SocketAsyncEventArgs();
            if (m_socketType == SocketType.Listen)
            {
                var networkCfg = SunNet.Instance.GetServerConfig().NetworkConfig;
                m_socket.ReceiveBufferSize = networkCfg.SocketInputBufferLen;
                //m_socket.SendBufferSize = networkCfg.SocketOutputBufferLen;
                m_event.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptComplete);
            }
            else if (m_socketType == SocketType.Normal)
            {
                m_event.UserToken = m_socket;
                m_event.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveComplete);
                var networkCfg = SunNet.Instance.GetServerConfig().NetworkConfig;
                m_socket.ReceiveBufferSize = networkCfg.SocketInputBufferLen;
                m_socket.SendBufferSize = networkCfg.SocketOutputBufferLen;
                if (bufferManager != null)
                {
                    bufferManager.SetBuffer(m_event);
                }
                else
                {
                    m_event.SetBuffer(receiveBuff, 0, receiveBuff.Length);
                }
            }

            m_taskCompleteEvent = new ManualResetEvent(false);
        }

        public void ConstructTask()
        {
            if (m_socketType == SocketType.Listen)
            {
                m_task = ConstructSocketAcceptTask();
            }
            else if (m_socketType == SocketType.Normal)
            {
                m_task = ConstructSocketReceiveTask();
            }
        }

        public Task<Conn> ConstructSocketReceiveTask()
        {
            var task = new Task<Conn>(() =>
            {
                //SunNet.Instance.Log.Info("Conn SocketReceiveTask Start");
                m_taskCompleteEvent.Reset();
                if (!m_socket.ReceiveAsync(m_event))
                {
                    ProcessReceive(m_event);
                }
                m_taskCompleteEvent.WaitOne();
                return this;
            });
            return task;
        }

        public Task<Conn> ConstructSocketAcceptTask()
        {
            m_event.AcceptSocket = null;
            var task = new Task<Conn>(() =>
            {
                //SunNet.Instance.Log.Info("Conn SocketAcceptTask Start");
                m_taskCompleteEvent.Reset();
                if (!m_socket.AcceptAsync(m_event))
                {
                    ProcessAccept(m_event);
                }
                m_taskCompleteEvent.WaitOne();
                return this;
            });
            return task;
        }


        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            //SunNet.Instance.Log.Info("Conn ProcessReceive:" + e.SocketError);
            m_taskCompleteEvent.Set(); 
        }

        private void OnReceiveComplete(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            //SunNet.Instance.Log.Info("Conn ProcessAccept:"+ e.SocketError);
            m_taskCompleteEvent.Set();
        }

        private void OnAcceptComplete(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        public override string ToString()
        {
            if(m_socketType == SocketType.Listen)
            {
                return string.Format("{0} {1}", m_socketType, m_socket.ToString());
            }else
            {
                return string.Format("{0} {1}", m_socketType, m_socket.RemoteEndPoint.ToString());
            }
        }

    }
}
