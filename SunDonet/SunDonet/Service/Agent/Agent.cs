using Google.Protobuf;
using MongoDB.Driver;
using SunDonet.DB;
using SunDonet.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class Agent : ServiceBase
    {
        public Agent(int id) : base(id) { }

        private PlayerContext m_playerContext;

        private string m_userId;
        private int m_gatewayId;

        public override void OnInit()
        {
            base.OnInit();
           
            RegisterServiceMsgCallHandler<S2SAgentInitReq, S2SAgentInitAck>(HandleAgentInitReq);
            RegisterServiceMsgNtfHandler<S2SClientMsgHandleNtf>(HandleClientNtf);
            RegisterServiceMsgCallHandler<S2SAgentExitReq, S2SAgentExitAck>(HandleAgentExitReq);

            m_playerContext = new PlayerContext();

            SetTickPeroid(0.5f);
            StartTick();
        }

        public override void OnExit()
        {
            Debug.Log("Agent:OnExit");
            base.OnExit();
            StopTick();
        }

        protected override async Task OnTick(float deltaTime)
        {
            //SunNet.Instance.Log.Info("Agent:OnTick");
            await m_playerContext.OnTick(deltaTime);
        }

        public void SendPackage(IMessage msg)
        {
            Gateway.SendPackage(m_gatewayId, this.m_id, msg);
        }

        public void SendPackageList(List<IMessage> msgList)
        {
            Gateway.SendPackageList(m_gatewayId, this.m_id, msgList);
        }

        private async Task<S2SAgentInitAck> HandleAgentInitReq(S2SAgentInitReq req)
        {
            SunNet.Instance.Log.Info(string.Format("Agent:HandleAgentInitReq thisId:{0} userid:{1} gateway:{2}",this.m_id, req.UserId, req.GatewayId));
            this.m_gatewayId = req.GatewayId;
            this.m_userId = req.UserId;
            m_playerContext.m_gameUserId = req.UserId;
            m_playerContext.SetAgent(this);
            await m_playerContext.OnLoginOK();
            return new S2SAgentInitAck() { };
        }

        private async Task HandleClientNtf(S2SClientMsgHandleNtf reqWarp)
        {
            var req = reqWarp.m_req;
            //SunNet.Instance.Log.Info(string.Format("Agent:HandleClientReq {0}", req));
            int id = SunNet.Instance.ProtocolDic.GetIdByType(req.GetType());
            switch (id)
            {
                case SunDonetProtocolDictionary.MsgId_PlayerInfoInitReq:
                    await HandleAgentInfoInitReq(req as PlayerInfoInitReq);
                    break;
            }
        }

        private async Task HandleAgentInfoInitReq(PlayerInfoInitReq req)
        {
            await m_playerContext.HandlePlayerInfoInitReq(req);
        }

        private async Task<S2SAgentExitAck> HandleAgentExitReq(S2SAgentExitReq req)
        {
            Debug.Log("Agent:HandlePlayerExitReq id:{2} userId: {0} gateway:{1}", m_userId, m_gatewayId,this.m_id);

            m_playerContext.Save2DB();

            SunNet.Instance.KillService(this.m_id);

            return new S2SAgentExitAck();
        }
    }
}
