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

            m_playerContext = new PlayerContext();

            SetTickPeroid(0.5f);
            StartTick();
        }

        protected override async Task OnTick(float deltaTime)
        {
            //Console.WriteLine("Agent:OnTick");
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
            Console.WriteLine(string.Format("Agent:HandleAgentInitReq {0}", req.m_userId));
            this.m_gatewayId = req.m_gatewayId;
            this.m_userId = req.m_userId;
            m_playerContext.m_gameUserId = req.m_userId;
            m_playerContext.SetAgent(this);
            await m_playerContext.OnLoginOK();
            return new S2SAgentInitAck() { };
        }

        private async Task HandleClientNtf(S2SClientMsgHandleNtf reqWarp)
        {
            var req = reqWarp.m_req;
            Console.WriteLine(string.Format("Agent:HandleClientReq {0}", req));
            int id = SunNet.Instance.ProtocolDic.GetIdByType(req.GetType());
            switch (id)
            {
                case SunDonetProtocolDictionary.MsgId_PlayerInfoInitReq:
                    await HandlePlayerInfoInitAck(req as PlayerInfoInitReq);
                    break;
            }
        }

        private async Task HandlePlayerInfoInitAck(PlayerInfoInitReq req)
        {
            await m_playerContext.HandlePlayerInfoInitAck(req);
        }
    }
}
