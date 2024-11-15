using kcp2k;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SunDonet
{
    public enum KcpState : byte
    {
        // don't react on 0x00. might help to filter out random noise.
        Reliable = 1,
        Unreliable = 2
    }

    public class KcpConn
    {
        // kcp reliability algorithm
        internal Kcp kcp;

        public Socket m_socket;

        internal uint cookie;

        // raw send buffer is exactly MTU.
        readonly byte[] rawSendBuffer;

        /// <summary>
        /// conn的创建服务id
        /// 目前只有gateway和admin会创建监听conn
        /// 监听conn接受连接得到的conn的m_serviceId和监听conn的一样
        /// </summary>
        public int m_serviceId;

        public SocketType m_socketType;
        public SocketAsyncEventArgs m_event;

        public byte[] receiveBuff = new byte[1024 * 5];
        public int m_receiveLen = 0;

        public Task<Conn> m_task;
        private ManualResetEvent m_taskCompleteEvent;

        public KcpConn(Socket socket, SocketType type, int serviceId = -1, BufferManager bufferManager = null)
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

        public void Reset()
        {

        }

        void KcpOutput(byte[] data, int length)
        {
            // write channel header
            // from 0, with 1 byte
            rawSendBuffer[0] = (byte)KcpState.Reliable;

            // write handshake cookie to protect against UDP spoofing.
            // from 1, with 4 bytes
            Utils.Encode32U(rawSendBuffer, 1, cookie); // allocation free

            // write data
            // from 5, with N bytes
            Buffer.BlockCopy(data, 0, rawSendBuffer, 1 + 4, length);

            // IO send
            ArraySegment<byte> segment = new ArraySegment<byte>(rawSendBuffer, 0, length + 1 + 4);
            UDPOutput(segment);
        }

        protected void UDPOutput(ArraySegment<byte> data)
        {
            if (m_socket == null) return;

            try
            {
                m_socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
            }
            catch (SocketException e)
            {
                SunNet.Instance.Log.Info($"[KCP] Client.RawSend: looks like the other end has closed the connection. This is fine: {e}");
            }
        }

        public ClientBuffer GetReceiveData()
        {
            if (m_event.SocketError != SocketError.Success)
                return null;
            if (m_event.BytesTransferred <= 0)
                return null;
            ClientBuffer buffer = ClientBuffer.GetBuffer(m_event.BytesTransferred);
            Array.Copy(m_event.Buffer, m_event.Offset, buffer.m_buffer, 0, m_event.BytesTransferred);
            buffer.m_dataLen = m_event.BytesTransferred;
            kcp.Input(m_event.Buffer, m_event.Offset, m_event.BytesTransferred);
            return buffer;
        }
    }
}
