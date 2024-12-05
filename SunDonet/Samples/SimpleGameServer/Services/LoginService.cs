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
            //查询是否已经登陆
            var loginStatusSearch = await Call<AgentSearchReq, AgentSearchAck>(m_agentMgrId, new AgentSearchReq()
            {
                UserId = userId
            });
            LoginAck ack = new LoginAck()
            {
                Result = ErrorCode.OK,
            };
            if (loginStatusSearch.Result == ErrorCode.OK) //已经是登陆状态
            {
                var registerItem = loginStatusSearch.RegisterItem;
                if (registerItem.Socket != socket)
                {
                    //可能是别的设备，重复登陆
                    //返回错误码，禁止其登陆
                    ack.Result = ErrorCode.LoginHasBeenLogin;
                }
                else
                {
                    //重复登陆，可能发生了状态错误
                    //返回错误码，禁止其登陆
                    ack.Result = ErrorCode.LoginMultiLogin;
                }
                GatewayService.SendPackage(ntf.GatewayId, ntf.Socket, ack);
                return;
            }
            else
            {
                //检测用户名，密码是否一致
                var dbAccount = await DBMethod.GetAccount(req.UserName);

                if (dbAccount == null)
                {
                    ack.Result = ErrorCode.LoginAccountNotExist;//用户不存在
                }
                else
                {
                    if (dbAccount.Password != req.UserPassword)
                    {
                        ack.Result = ErrorCode.LoginPasswordError;//密码错误
                    }
                }
                if (ack.Result == ErrorCode.OK)
                {
                    //登录成功，创建对应的agentService对象
                    var agentId = SimpleGameServer.Instance.NewService("Agent", null);
                    //注册agent到agentMgr
                    await Call<AgentRegisterReq, AgentRegisterAck>(m_agentMgrId, new AgentRegisterReq()
                    {
                        AgentId = agentId,
                        UserId = userId,
                        GatewayId = ntf.GatewayId,
                        Socket = ntf.Socket,
                    });
                    //agent初始化
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
            //查询是否在登录状态
            var searchResult = await Call<AgentSearchReq, AgentSearchAck>(m_agentMgrId, new AgentSearchReq()
            {
                Socket = ntf.Socket,
            });
            if (searchResult != null && searchResult.Result == ErrorCode.OK)
            {
                var agentId = searchResult.RegisterItem.AgentId;
                //agentService执行退出流程：数据存盘，停止自身...
                var ack = await Call<AgentExitReq, AgentExitAck>(agentId, new AgentExitReq());
                //将agent从agentMgr中解除
                await Call<AgentRemoveReq, AgentRemoveAck>(m_agentMgrId, new AgentRemoveReq()
                {
                    AgentId = agentId,
                });
            }
        }
    }
}
