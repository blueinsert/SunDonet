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
            RegisterServiceMsgNtfHandler<S2SLogoutNtf>(HandleLogoutNtf);
        }

        private async Task HandleLoginNtf(S2SLoginNtf ntf)
        {
            Debug.Log("Login:HandleLoginReq {0} {1}", ntf.Socket.RemoteEndPoint, ntf.Req.UserName);
            var req = ntf.Req;
            var userId = req.UserName;
            var password = req.UserPassword;
            var socket = ntf.Socket;
            var loginStatusSearch = await Call<S2SAgentSearchReq, S2SAgentSearchAck>(m_agentMgrId, new S2SAgentSearchReq()
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
                Gateway.SendPackage(ntf.GatewayId, ntf.Socket, ack);
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
                    await Call<S2SAgentRegisterReq, S2SAgentRegisterAck>(m_agentMgrId, new S2SAgentRegisterReq() {
                        AgentId = agentId,
                        UserId = userId,
                        GatewayId = ntf.GatewayId,
                        Socket = ntf.Socket,
                    });
                    await Call<S2SAgentInitReq, S2SAgentInitAck>(agentId, new S2SAgentInitReq()
                    {
                        UserId = req.UserName,
                        GatewayId = ntf.GatewayId,
                    });
                }
                Gateway.SendPackage(ntf.GatewayId, ntf.Socket, ack);
                return;
            }  
        }

        private async Task HandleCreateAccountNtf(S2SCreateAccountNtf ntf)
        {
            SunNet.Instance.Log.Info("LoginService:HandleCreateAccountReq");
            var userId = ntf.Req.UserName;
            var password = ntf.Req.UserPassword;

            CreateAccountAck ack = new CreateAccountAck()
            {
                Result = ErrorCode.OK,
            };

            var res = await DBMethod.CreateAccount(userId, password);

            Gateway.SendPackage(ntf.GatewayId, ntf.Socket, ack);
        }

        private async Task HandleLogoutNtf(S2SLogoutNtf ntf)
        {
            Debug.Log("Login:HandleLogoutNtf:{0}", ntf.Socket.RemoteEndPoint.ToString());
            var searchResult = await Call<S2SAgentSearchReq, S2SAgentSearchAck>(m_agentMgrId, new S2SAgentSearchReq()
            {
                Socket = ntf.Socket,
            });
            if (searchResult != null && searchResult.Result == ErrorCode.OK)
            {
                var agentId = searchResult.RegisterItem.AgentId;
                var ack = await Call<S2SAgentExitReq, S2SAgentExitAck>(agentId, new S2SAgentExitReq());
                await Call<S2SAgentRemoveReq, S2SAgentRemoveAck>(m_agentMgrId, new S2SAgentRemoveReq()
                {
                    AgentId = agentId,
                });
                //SunNet.Instance.CloseConn(ntf.Socket);
            }   
        }
    }
}
