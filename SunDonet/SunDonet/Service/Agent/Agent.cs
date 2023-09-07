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
        }

        private async Task<S2SAgentInitAck> HandleAgentInitReq(S2SAgentInitReq req)
        {
            Console.WriteLine(string.Format("Agent:HandleAgentInitReq {0}", req.m_userId));
            m_playerContext.m_gameUserId = req.m_userId;
            await m_playerContext.OnLoginOK();
            return new S2SAgentInitAck() { };
        }
    }
}
