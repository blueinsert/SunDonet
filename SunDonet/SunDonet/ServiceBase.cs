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

        public virtual async Task OnClientConnect(Socket s)
        {

        }

        public virtual async Task OnClientData(Socket s, byte[] data) { }

        public virtual async Task OnClientDisconnect(Socket s)
        {

        }

        public virtual async Task OnServiceMsg(ServiceMsg msg)
        {
            
        }

        public virtual async Task<ServiceMsgAck> OnLocalServiceMsgWithReturn(ServiceMsgReq Req)
        {
            return null;
        }

        private async Task OnMsg(MsgBase msg) {
            if(msg.m_type == MsgBase.MsgType.Service)
            {
                if(msg is ServiceMsgReq)
                {
                    var req = msg as ServiceMsgReq;
                    var token = req.m_token;
                    var ack = await OnLocalServiceMsgWithReturn(req);
                    ack.m_token = token;
                    SunNet.Instance.SetAck(-1, ack);
                }
                else
                {
                    await OnServiceMsg(msg as ServiceMsg);
                }
            }
            switch (msg.m_type)
            {
                case MsgBase.MsgType.Socket_Accept:
                    await OnClientConnect((msg as SocketAcceptMsg).m_client);
                    break;
                case MsgBase.MsgType.Socket_Disconnect:
                    await OnClientDisconnect((msg as SocketDisconnectMsg).m_client);
                    break;
                case MsgBase.MsgType.Socket_Data:
                    var clientDataMsg = msg as SocketDataMsg;
                    await OnClientData(clientDataMsg.m_socket, clientDataMsg.m_data);
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

        private async Task<bool> ProcessMsg()
        {
            var msg = PopMsg();
            if (msg != null)
            {
                await OnMsg(msg);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task ProcessMsgs(int max)
        {
            for(int i = 0; i < max; i++)
            {
                if (! (await ProcessMsg()))
                    break;
            }
        }
    }
}
