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

    /// <summary>
    /// task级 线程级 service
    /// </summary>
    public abstract class ServiceBase
    {
        public int m_id;
        public object m_msgQueueLock = new object();
        /// <summary>
        /// 消息队列
        /// </summary>
        public Queue<MsgBase> m_msgQueue = new Queue<MsgBase>();

        public object m_isInGlobalLock = new object();
        public bool m_isInGlobal = false;

        private Dictionary<Type, ServericeCallHandleDelegate<ServiceMsgReq,ServiceMsgAck>> m_msgCallHandleDictionary = new Dictionary<Type, SunDonet.ServericeCallHandleDelegate<ServiceMsgReq, ServiceMsgAck>>();
        private Dictionary<Type, KeyValuePair<Type, Type>> m_callableMsgTypePair = new Dictionary<Type, KeyValuePair<Type, Type>>();

        private Dictionary<Type, ServericeMsgHandleDelegate<ServiceMsgNtf>> m_msgNtfHandleDictionary = new Dictionary<Type, ServericeMsgHandleDelegate<ServiceMsgNtf>>();

        /// <summary>
        /// 启动参数字典
        /// </summary>
        private Dictionary<string, string> m_paramDic = new Dictionary<string, string>();

        private float m_tickPeroid = 1.0f;

        private DateTime m_lastTickTime;

        private long m_timerTickId = -1;

        public void SetParams(Dictionary<string, string> paramDic)
        {
            m_paramDic = paramDic;
        }

        public string GetStringParam(string key)
        {
            if (m_paramDic.ContainsKey(key))
            {
                return m_paramDic[key];
            }
            return null;
        }

        public int GetIntParam(string key)
        {
            if (m_paramDic.ContainsKey(key))
            {
                var value = m_paramDic[key];
                if(int.TryParse(value,out var res))
                {
                    return res;
                }
            }
            return -1;
        }

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
            //添加定时驱动
            m_timerTickId = SunNet.Instance.Timer.AddTimer(ServerOnTickTimerCallBack, this, 0, (int)(m_tickPeroid*1000));
        }

        protected void StopTick()
        {
            if(m_timerTickId != -1)
                SunNet.Instance.Timer.RemoveTimer(m_timerTickId);
        }

        /// <summary>
        /// 消息驱动的定时tick
        /// 定时器不直接调用tick，而是添加一条消息
        /// </summary>
        /// <param name="obj"></param>
        private void ServerOnTickTimerCallBack(Object obj)
        {
           Send(this.m_id, ServiceTickMsgNtf.TickMsgNtf);
        }

        protected virtual async Task OnTick(float deltaTime) { }

        public virtual void OnInit() {
            //在这里初始化每个service的消息响应函数
            //这个是对定时tick消息的响应
            RegisterServiceMsgNtfHandler<ServiceTickMsgNtf>(async (msg) => {
                var now = DateTime.Now;
                float deltaTime = (float)(now - m_lastTickTime).TotalSeconds;
                await OnTick(deltaTime);
            });
        }   

        public virtual void OnExit() {
            Debug.Log("ServiceBase:OnExit {0}", this.GetType().Name);
        }

        /// <summary>
        /// 注册消息处理函数
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TAck"></typeparam>
        /// <param name="handler"></param>
        protected void RegisterServiceMsgCallHandler<TReq, TAck>(ServericeCallHandleDelegate<TReq,TAck> handler) where TReq : ServiceMsgReq where TAck : ServiceMsgAck
        {
            m_callableMsgTypePair.Add(typeof(TReq), new KeyValuePair<Type, Type>(typeof(TReq), typeof(TAck)));
            m_msgCallHandleDictionary.Add(typeof(TReq), async (req)=> {
                var ack = await handler(req as TReq) as TAck;
                return ack;
            });
        }

        /// <summary>
        /// 注册消息处理函数
        /// </summary>
        /// <typeparam name="TMsg"></typeparam>
        /// <param name="handler"></param>
        protected void RegisterServiceMsgNtfHandler<TMsg>(ServericeMsgHandleDelegate<TMsg> handler) where TMsg : ServiceMsgNtf
        {
            m_msgNtfHandleDictionary.Add(typeof(TMsg), async (msg) => {
                await handler(msg as TMsg);
            });
        }

        /// <summary>
        /// 向另一个服务发送ntf消息
        /// </summary>
        /// <param name="to"></param>
        /// <param name="msg"></param>
        protected void Send(int to, ServiceMsgNtf msg)
        {
            msg.Source = this.m_id;
            SunNet.Instance.Send(to, msg);
        }

        /// <summary>
        /// 向另一个服务发送协作式消息(需要等待对方完成)
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TAck"></typeparam>
        /// <param name="to"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        public async Task<TAck> Call<TReq, TAck>(int to, TReq req) where TReq : ServiceMsgReq where TAck : ServiceMsgAck
        {
            req.Source = this.m_id;
            return await SunNet.Instance.Call<TReq, TAck>(to, req);
        }

        private async Task OnServiceMsg(ServiceMsgNtf msg)
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
            //服务间消息
            if(msg.MessageType == MsgBase.MsgType.Service2Service)
            {
                //协作式消息
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
                //ntf消息
                else if(msg is ServiceMsgNtf)
                {
                    await OnServiceMsg(msg as ServiceMsgNtf);
                }
                return;
            }
            
            if(msg.MessageType == MsgBase.MsgType.Socket_Accept
                || msg.MessageType == MsgBase.MsgType.Socket_Disconnect
                || msg.MessageType == MsgBase.MsgType.Socket_Data)
            {
                await OnSocketMsg(msg);
            } 
        }

        /// <summary>
        /// 只有网关服务需要实现该方法
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual async Task OnSocketMsg(MsgBase msg)
        {

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
