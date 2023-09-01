using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public abstract class ServiceBase
    {
        public int m_id;
        public object m_msgQueueLock = new object();
        public Queue<MsgBase> m_msgQueue = new Queue<MsgBase>();
        public bool m_isInGlobal = false;

        public virtual void OnInit() { }   
        public virtual void OnExit() { }

        public virtual void OnClientConnect(Socket s)
        {

        }

        public virtual void OnClientData(Socket s, byte[] data) { }

        public virtual void OnClientDisconnect(Socket s)
        {

        }

        public virtual void OnServiceMsg(ServiceMsg msg)
        {

        }

        private void OnMsg(MsgBase msg) {
            switch (msg.m_type)
            {
                case MsgBase.MsgType.Service:
                    OnServiceMsg(msg as ServiceMsg);
                    break;
                case MsgBase.MsgType.Socket_Accept:
                    OnClientConnect((msg as SocketAcceptMsg).m_client);
                    break;
                case MsgBase.MsgType.Socket_Disconnect:
                    OnClientDisconnect((msg as SocketDisconnectMsg).m_client);
                    break;
                case MsgBase.MsgType.Socket_Data:
                    var clientDataMsg = msg as SocketDataMsg;
                    OnClientData(clientDataMsg.m_socket, clientDataMsg.m_data);
                    break;
            }
        }

        public ServiceBase(int id)
        {
            m_id = id;
        }

        public void PushMsg(MsgBase msg)
        {
            lock (m_msgQueueLock)
            {
                m_msgQueue.Enqueue(msg);
            }
        }

        private MsgBase PopMsg()
        {
            MsgBase msg = null;
            lock (m_msgQueueLock)
            {
                if (m_msgQueue.Count != 0)
                {
                    msg = m_msgQueue.Dequeue();
                }
            }
            return msg;
        }

        private bool ProcessMsg()
        {
            var msg = PopMsg();
            if (msg != null)
            {
                OnMsg(msg);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ProcessMsgs(int max)
        {
            for(int i = 0; i < max; i++)
            {
                if (!ProcessMsg())
                    break;
            }
        }
    }
}
