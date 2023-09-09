using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SunDonet.DB;
using SunDonet.Protocol;

namespace SunDonet
{

    public class Login : ServiceBase
    {
        int m_agentMgrId;

        public Login(int id) : base(id)
        {

        }

        public override void OnInit()
        {
            base.OnInit();
            m_agentMgrId = SunNet.Instance.FindSingletonServiceByName("AgentMgr");
            RegisterServiceMsgNtfHandler<S2SLoginNtf>(HandleLoginNtf);
            RegisterServiceMsgNtfHandler<S2SCreateAccountNtf>(HandleCreateAccountNtf);
        }

        private async Task HandleLoginNtf(S2SLoginNtf ntf)
        {
            //Console.WriteLine("LoginService:HandleLoginReq");
            var req = ntf.m_req;
            var userId = req.UserName;
            var password = req.UserPassword;
            var loginStatusSearch = await SunNet.Instance.Call<S2SAgentSearchReq, S2SAgentSearchAck>(m_agentMgrId, new S2SAgentSearchReq()
            {
                UserId = userId
            });
            LoginAck ack = new LoginAck()
            {
                Result = ErrorCode.OK,
            };
            if (loginStatusSearch.Result == ErrorCode.OK)
            {
                Gateway.SendPackage(ntf.m_gatewayId, ntf.m_socket, ack);
                return;
            }
            else
            {
                var dbAccount = await DBMethod.GetAccount(req.UserName);

                if (dbAccount == null)
                {
                    ack.Result = ErrorCode.LoginAccountNotExist;
                }
                else
                {
                    if (dbAccount.Password != req.UserPassword)
                    {
                        ack.Result = ErrorCode.LoginPasswordError;
                    }
                }
                if (ack.Result == ErrorCode.OK)
                {
                    var agentId = SunNet.Instance.NewService("Agent");
                    await SunNet.Instance.Call<S2SAgentRegisterReq, S2SAgentRegisterAck>(m_agentMgrId, new S2SAgentRegisterReq() {
                        AgentId = agentId,
                        UserId = userId,
                        GatewayId = ntf.m_gatewayId,
                        Socket = ntf.m_socket,
                    });
                    await SunNet.Instance.Call<S2SAgentInitReq, S2SAgentInitAck>(agentId, new S2SAgentInitReq()
                    {
                        m_userId = req.UserName,
                    });
                }
                Gateway.SendPackage(ntf.m_gatewayId, ntf.m_socket, ack);
                return;
            }  
        }

        private async Task HandleCreateAccountNtf(S2SCreateAccountNtf ntf)
        {
            Console.WriteLine("LoginService:HandleCreateAccountReq");
            var userId = ntf.m_req.UserName;
            var password = ntf.m_req.UserPassword;

            CreateAccountAck ack = new CreateAccountAck()
            {
                Result = ErrorCode.OK,
            };

            var res = await DBMethod.CreateAccount(userId, password);

            Gateway.SendPackage(ntf.m_gatewayId, ntf.m_socket, ack);
        }
    }
}
