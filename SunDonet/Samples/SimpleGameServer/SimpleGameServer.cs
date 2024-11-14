using bluebean.SimpleGameServer.ServiceProtocol;
using SunDonet;
using SunDonet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bluebean.SimpleGameServer
{
    public class SimpleGameServer : SunNet
    {

        public static new SimpleGameServer CreateInstance()
        {
            var instance = new SimpleGameServer();

            m_instance = instance;

            return instance;
        }

        public static new SimpleGameServer Instance { get { return m_instance as SimpleGameServer; } }

        protected override void InitClientProtocolDic()
        {
            m_protocolDic = new SunDonetProtocolDictionary();
        }

        protected override void OnStop()
        {
            base.OnStop();
            Debug.Log("SunNet:Stop");
            //todo
            //Interlocked.CompareExchange
            ServerState = ServerState.Stoping;
            //1.停止接受新的连接
            m_socketWorker.Stop();
            Thread.Sleep(500);
            //2.将所有agent踢下线
            int agentMgr = FindSingletonServiceByName("AgentMgr");
            int login = FindSingletonServiceByName("Login");
            var agetMgrIns = GetService(agentMgr) as AgentMgrService;
            var agents = agetMgrIns.RegisterItemList;
            foreach (var agent in agents)
            {
                Send(login, new LogoutNtf()
                {
                    GatewayId = agent.GateWayId,
                    Socket = agent.Socket,
                });
            }
            while (agents.Count != 0)
            {
                Thread.Sleep(100);
            }
            Debug.Log("agents.Count == 0, all user has been kickout");
            Uninitialize();
            ServerState = ServerState.Stoped;

            DoExit();
        }
    }
}
