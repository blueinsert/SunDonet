using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public delegate Task<TAck> ServericeCallHandleDelegate<TReq, TAck>(TReq req) where TReq : ServiceMsgReq where TAck : ServiceMsgAck;
    public delegate Task ServericeMsgHandleDelegate<TReq>(TReq req) where TReq : ServiceMsgNtf;

    public abstract class ServiceBase
    {
        public int m_id;
        public object m_msgQueueLock = new object();
        public Queue<MsgBase> m_msgQueue = new Queue<MsgBase>();
        public object m_isInGlobalLock = new object();
        public bool m_isInGlobal = false;

        private Dictionary<Type, ServericeCallHandleDelegate<ServiceMsgReq,ServiceMsgAck>> m_msgCallHandleDictionary = new Dictionary<Type, SunDonet.ServericeCallHandleDelegate<ServiceMsgReq, ServiceMsgAck>>();
        private Dictionary<Type, KeyValuePair<Type, Type>> m_callableMsgTypePair = new Dictionary<Type, KeyValuePair<Type, Type>>();

        private Dictionary<Type, ServericeMsgHandleDelegate<ServiceMsgNtf>> m_msgNtfHandleDictionary = new Dictionary<Type, ServericeMsgHandleDelegate<ServiceMsgNtf>>();

        private float m_tickPeroid = 1.0f;

        private DateTime m_lastTickTime;

        /// <summary>
        /// second
        /// </summary>
        /// <param name="s"></param>
        protected void SetTickPeroid(float s)
        {
            m_tickPeroid = s;
            m_lastTickTime = DateTime.Now;
        }

        protected void StartTick()
        {
            SunNet.Instance.Timer.AddTimer(ServerOnTickTimerCallBack, this, 0, (int)(m_tickPeroid*1000));
        }

        private void ServerOnTickTimerCallBack(Object obj)
        {
            SunNet.Instance.Send(this.m_id, ServiceTickMsgNtf.TickMsgNtf);
        }

        protected virtual async Task OnTick(float deltaTime) { }

        public virtual void OnInit() {
            RegisterServiceMsgNtfHandler<ServiceTickMsgNtf>(async (msg) => {
                var now = DateTime.Now;
                float deltaTime = (float)(now - m_lastTickTime).TotalSeconds;
                await OnTick(deltaTime);
            });
        }   

        public virtual void OnExit() { }

        protected void RegisterServiceMsgCallHandler<TReq, TAck>(ServericeCallHandleDelegate<TReq,TAck> handler) where TReq : ServiceMsgReq where TAck : ServiceMsgAck
        {
            m_callableMsgTypePair.Add(typeof(TReq), new KeyValuePair<Type, Type>(typeof(TReq), typeof(TAck)));
            m_msgCallHandleDictionary.Add(typeof(TReq), async (req)=> {
                var ack = await handler(req as TReq) as TAck;
                return ack;
            });
        }

        protected void RegisterServiceMsgNtfHandler<TMsg>(ServericeMsgHandleDelegate<TMsg> handler) where TMsg : ServiceMsgNtf
        {
            m_msgNtfHandleDictionary.Add(typeof(TMsg), async (msg) => {
                await handler(msg as TMsg);
            });
        }

        public virtual async Task OnClientConnect(Socket s)
        {

        }

        public virtual async Task OnClientData(Socket s, ClientBuffer buff) { }

        public virtual async Task OnClientDisconnect(Socket s)
        {

        }

        public virtual async Task OnServiceMsg(ServiceMsgNtf msg)
        {
            ServericeMsgHandleDelegate<ServiceMsgNtf> callback = null;
            if (m_msgNtfHandleDictionary.TryGetValue(msg.GetType(), out callback))
            {
                 await callback(msg);
            }
            else
            {
                SunNet.Instance.Log.Info(string.Format("{0} OnServiceMsg msg:{1} not find handler", this.GetType().Name, msg.GetType().Name));
            }
        }

        private async Task<ServiceMsgAck> OnServiceCall(ServiceMsgReq req)
        {
            ServericeCallHandleDelegate<ServiceMsgReq, ServiceMsgAck> callback = null;
            if(m_msgCallHandleDictionary.TryGetValue(req.GetType(), out callback))
            {
                return await callback(req);
            }
            else
            {
                SunNet.Instance.Log.Info(string.Format("{0} OnServiceCall msg:{1} not find handler", this.GetType().Name, req.GetType().Name));
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
                        SunNet.Instance.Log.ErrorFormat("OnServiceCall req:{0} ruturn null", req.GetType());
                    }
                    ack.m_token = token;
                    SunNet.Instance.SetAck(-1, ack);
                }
                else if(msg is ServiceMsgNtf)
                {
                    await OnServiceMsg(msg as ServiceMsgNtf);
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
                SunNet.Instance.Log.Info(string.Format("ServiceBase:ProcessMsgs exception: {0} \n{1}", e.ToString(), e.StackTrace));
            }
        }
    }
}
