using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class AgentRegisterItem
    {
        public string UserId;
        public int AgentId;
        public int GateWayId;
        public SocketIndentifier Socket;
    }

    /// <summary>
    /// 查询，注册 agent
    /// </summary>
    public class AgentMgr : ServiceBase
    {
        public List<AgentRegisterItem> RegisterItemList { get { return m_registerItemList; } }
        private List<AgentRegisterItem> m_registerItemList = new List<AgentRegisterItem>();
        private Dictionary<int, AgentRegisterItem> m_agent2ItemDic = new Dictionary<int, AgentRegisterItem>();
        private Dictionary<string, AgentRegisterItem> m_userId2ItemDic = new Dictionary<string, AgentRegisterItem>();
        private Dictionary<SocketIndentifier, AgentRegisterItem> m_socket2ItemDic = new Dictionary<SocketIndentifier, AgentRegisterItem>();

        public AgentMgr(int id) : base(id) { }

        public override void OnInit()
        {
            base.OnInit();
            RegisterServiceMsgCallHandler<S2SAgentSearchReq, S2SAgentSearchAck>(HandleSearchReq);
            RegisterServiceMsgCallHandler<S2SAgentRegisterReq, S2SAgentRegisterAck>(HandleRegisterReq);
            RegisterServiceMsgCallHandler<S2SAgentRemoveReq, S2SAgentRemoveAck>(HandlerRemoveReq);
        }

        private async Task<S2SAgentSearchAck> HandleSearchReq(S2SAgentSearchReq req)
        {
            //SunNet.Instance.Log.Info(string.Format("AgentMgr:HandleSearchReq req:{0}", req));
            var ack = new S2SAgentSearchAck();
            ack.Result = 1;//todo
            ack.RegisterItem = null;
            if (!string.IsNullOrEmpty(req.UserId))
            {
                var userId = req.UserId;
                if (m_userId2ItemDic.ContainsKey(userId))
                {
                    ack.Result = ErrorCode.OK;
                    ack.RegisterItem = m_userId2ItemDic[userId];
                }
            }else if (req.AgentId != -1)
            {
                var agentId = req.AgentId;
                if (m_agent2ItemDic.ContainsKey(agentId))
                {
                    ack.Result = ErrorCode.OK;
                    ack.RegisterItem = m_agent2ItemDic[agentId];
                }
            }else if (req.Socket != null)
            {
                var socket = req.Socket;
                if (m_socket2ItemDic.ContainsKey(socket))
                {
                    ack.Result = ErrorCode.OK;
                    ack.RegisterItem = m_socket2ItemDic[socket];
                }
            }
           
            return ack;
        }

        private async Task<S2SAgentRegisterAck> HandleRegisterReq(S2SAgentRegisterReq req)
        {
            Debug.Log(string.Format("AgentMgr:HandleRegisterReq req:{0}", req));
            var ack = new S2SAgentRegisterAck();
            var item = new AgentRegisterItem()
            {
                UserId = req.UserId,
                AgentId = req.AgentId,
                GateWayId = req.GatewayId,
                Socket = req.Socket,
            };
            m_agent2ItemDic.Add(req.AgentId, item);
            m_userId2ItemDic.Add(req.UserId, item);
            m_socket2ItemDic.Add(req.Socket, item);
            m_registerItemList.Add(item);
            return ack;
        }

        private async Task<S2SAgentRemoveAck> HandlerRemoveReq(S2SAgentRemoveReq req)
        {
             Debug.Log(string.Format("AgentMgr:HandlerRemoveReq req:{0}", req));
            var agentId = req.AgentId;
            if (m_agent2ItemDic.ContainsKey(agentId))
            {
                var item = m_agent2ItemDic[agentId];
                m_userId2ItemDic.Remove(item.UserId);
                m_agent2ItemDic.Remove(agentId);
                m_socket2ItemDic.Remove(item.Socket);
                m_registerItemList.Remove(item);
            }
            var ack = new S2SAgentRemoveAck();
            return ack;
        }
    }
}
