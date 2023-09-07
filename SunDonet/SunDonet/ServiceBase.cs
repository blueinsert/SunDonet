using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public delegate Task<TAck> ServericeCallHandleDelegateFullType<TReq, TAck>(TReq req) where TReq : ServiceMsgReq where TAck : ServiceMsgAck;
    public delegate Task<ServiceMsgAck> ServericeCallHandleDelegate(ServiceMsgReq req);
    public delegate void ServericeMsgHandleDelegate<TReq>(TReq req) where TReq : ServiceMsgNtf;

    public abstract class ServiceBase
    {
        public int m_id;
        public object m_msgQueueLock = new object();
        public Queue<MsgBase> m_msgQueue = new Queue<MsgBase>();
        public bool m_isInGlobal = false;

        private Dictionary<Type, ServericeCallHandleDelegate> m_msgCallHandleDictionary = new Dictionary<Type, SunDonet.ServericeCallHandleDelegate>();
        private Dictionary<Type, KeyValuePair<Type, Type>> m_callableMsgTypePair = new Dictionary<Type, KeyValuePair<Type, Type>>();

        public virtual void OnInit() { }   
        public virtual void OnExit() { }

        protected void RegisterMsgCallHandler<TReq, TAck>(ServericeCallHandleDelegate handler) where TReq : ServiceMsgReq where TAck : ServiceMsgAck
        {
            m_callableMsgTypePair.Add(typeof(TReq), new KeyValuePair<Type, Type>(typeof(TReq), typeof(TAck)));
            //var h = handler as ServericeCallHandleDelegate<ServiceMsgReq, ServiceMsgAck>;
            m_msgCallHandleDictionary.Add(typeof(TReq), handler);
        }

        protected void RegisterMsgCallHandler<TReq, TAck>(ServericeCallHandleDelegateFullType<TReq,TAck> handler) where TReq : ServiceMsgReq where TAck : ServiceMsgAck
        {
            m_callableMsgTypePair.Add(typeof(TReq), new KeyValuePair<Type, Type>(typeof(TReq), typeof(TAck)));
            //var h = handler as ServericeCallHandleDelegate<ServiceMsgReq, ServiceMsgAck>;
            m_msgCallHandleDictionary.Add(typeof(TReq), async (req)=> {
                var ack = await handler(req as TReq) as TAck;
                return ack;
            });
        }

        public virtual async Task OnClientConnect(Socket s)
        {

        }

        public virtual async Task OnClientData(Socket s, ClientBuffer buff) { }

        public virtual async Task OnClientDisconnect(Socket s)
        {

        }

        public virtual async Task OnServiceMsg(ServiceMsg msg)
        {
            
        }

        private async Task<ServiceMsgAck> OnServiceCall(ServiceMsgReq req)
        {
            ServericeCallHandleDelegate callback = null;
            if(m_msgCallHandleDictionary.TryGetValue(req.GetType(), out callback))
            {
                return await callback(req);
            }
            else
            {
                Console.WriteLine(string.Format("{0} OnServiceCall msg:{1} not find handler", this.GetType().Name, req.GetType().Name));
                return null;
            }
        }

        private async Task OnMsg(MsgBase msg) {
            if(msg.m_type == MsgBase.MsgType.Service)
            {
                if(msg is ServiceMsgReq)
                {
                    var req = msg as ServiceMsgReq;
                    var token = req.m_token;
                    var ack = await OnServiceCall(req);
                    if (ack == null)
                    {
                        ack = new NullServiceMsgAck();
                        Console.WriteLine("OnServiceCall req:{0} ruturn null", req.GetType());
                    }
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
                    await OnClientData(clientDataMsg.m_socket, clientDataMsg.m_buff);
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
            try
            {
                for (int i = 0; i < max; i++)
                {
                    if (!(await ProcessMsg()))
                        break;
                }
            }catch(Exception e)
            {
                Console.WriteLine(string.Format("ServiceBase:ProcessMsgs exception: {0} \n{1}", e.ToString(), e.StackTrace));
            }
        }
    }
}
