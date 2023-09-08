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

        public override void OnInit()
        {
            base.OnInit();
           
            RegisterServiceMsgCallHandler<S2SAgentInitReq, S2SAgentInitAck>(HandleAgentInitReq);

            m_playerContext = new PlayerContext();

            SetTickPeroid(0.5f);
            StartTick();
        }

        protected override async Task OnTick(float deltaTime)
        {
            //Console.WriteLine("Agent:OnTick");
            await m_playerContext.OnTick(deltaTime);
        }

        private async Task<S2SAgentInitAck> HandleAgentInitReq(S2SAgentInitReq req)
        {
            Console.WriteLine(string.Format("Agent:HandleAgentInitReq {0}", req.m_userId));
            m_playerContext.m_gameUserId = req.m_userId;
            await m_playerContext.OnLoginOK();
            return new S2SAgentInitAck() { };
        }

        private async Task<S2SClientMsgHandleAck> HandleClientReq(S2SClientMsgHandleReq req)
        {
            Console.WriteLine(string.Format("Agent:HandleClientReq {0}", req.m_req));
            int id = SunNet.Instance.ProtocolDic.GetIdByType(req.m_req.GetType());
            S2SClientMsgHandleAck ack = null;
            switch (id)
            {
                case SunDonetProtocolDictionary.MsgId_PlayerInfoInitReq:
                    ack = await HandlePlayerInfoInitAck(req.m_req as PlayerInfoInitReq);
                    break;
            }
            if (ack == null)
            {
                Console.WriteLine("Agent:HandleClientReq ack==null");
            }
            return ack;
        }

        private async Task<S2SClientMsgHandleAck> HandlePlayerInfoInitAck(PlayerInfoInitReq req)
        {
            return await m_playerContext.HandlePlayerInfoInitAck(req);
        }
    }
}
