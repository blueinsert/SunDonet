using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SunDonet.DB;
using SunDonet.Protocol;
using SunDonet;
using bluebean.SimpleGameServer.ServiceProtocol;
using SimpleGameServer.Protocol;

namespace bluebean.SimpleGameServer
{

    public class LoginService : ServiceBase
    {
        int m_agentMgrId;

        public LoginService(int id) : base(id)
        {

        }

        public override void OnInit()
        {
            base.OnInit();
            m_agentMgrId = SimpleGameServer.Instance.FindSingletonServiceByName("AgentMgr");
            RegisterServiceMsgNtfHandler<LoginNtf>(HandleLoginNtf);
            RegisterServiceMsgNtfHandler<CreateAccountNtf>(HandleCreateAccountNtf);
            RegisterServiceMsgNtfHandler<LogoutNtf>(HandleLogoutNtf);
        }

        private async Task HandleLoginNtf(LoginNtf ntf)
        {
            Debug.Log("Login:HandleLoginReq {0} {1}", ntf.Socket, ntf.Req.UserName);
            var req = ntf.Req;
            var userId = req.UserName;
            var password = req.UserPassword;
            var socket = ntf.Socket;
            var loginStatusSearch = await Call<AgentSearchReq, AgentSearchAck>(m_agentMgrId, new AgentSearchReq()
            {
                UserId = userId
            });
            LoginAck ack = new LoginAck()
            {
                Result = ErrorCode.OK,
            };
            if (loginStatusSearch.Result == ErrorCode.OK)
            {
                var registerItem = loginStatusSearch.RegisterItem;
                if (registerItem.Socket != socket)
                {
                    ack.Result = ErrorCode.LoginHasBeenLogin;
                }
                else
                {
                    ack.Result = ErrorCode.LoginMultiLogin;
                }
                GatewayService.SendPackage(ntf.GatewayId, ntf.Socket, ack);
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
                    var agentId = SimpleGameServer.Instance.NewService("Agent", null);
                    await Call<AgentRegisterReq, AgentRegisterAck>(m_agentMgrId, new AgentRegisterReq()
                    {
                        AgentId = agentId,
                        UserId = userId,
                        GatewayId = ntf.GatewayId,
                        Socket = ntf.Socket,
                    });
                    await Call<AgentInitReq, AgentInitAck>(agentId, new AgentInitReq()
                    {
                        UserId = req.UserName,
                        GatewayId = ntf.GatewayId,
                    });
                }
                GatewayService.SendPackage(ntf.GatewayId, ntf.Socket, ack);
                return;
            }
        }

        private async Task HandleCreateAccountNtf(CreateAccountNtf ntf)
        {
            SimpleGameServer.Instance.Log.Info("LoginService:HandleCreateAccountReq");
            var userId = ntf.Req.UserName;
            var password = ntf.Req.UserPassword;

            CreateAccountAck ack = new CreateAccountAck()
            {
                Result = ErrorCode.OK,
            };

            var res = await DBMethod.CreateAccount(userId, password);

            GatewayService.SendPackage(ntf.GatewayId, ntf.Socket, ack);
        }

        private async Task HandleLogoutNtf(LogoutNtf ntf)
        {
            Debug.Log("Login:HandleLogoutNtf:{0}", ntf.Socket.ToString());
            var searchResult = await Call<AgentSearchReq, AgentSearchAck>(m_agentMgrId, new AgentSearchReq()
            {
                Socket = ntf.Socket,
            });
            if (searchResult != null && searchResult.Result == ErrorCode.OK)
            {
                var agentId = searchResult.RegisterItem.AgentId;
                var ack = await Call<AgentExitReq, AgentExitAck>(agentId, new AgentExitReq());
                await Call<AgentRemoveReq, AgentRemoveAck>(m_agentMgrId, new AgentRemoveReq()
                {
                    AgentId = agentId,
                });
            }
        }
    }
}
