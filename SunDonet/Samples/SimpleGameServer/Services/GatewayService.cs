using SunDonet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using bluebean.SimpleGameServer.ServiceProtocol;
using SunDonet.Protocol;

namespace bluebean.SimpleGameServer
{
    public class GatewayService : GatewayServiceBase
    {
        public GatewayService(int id) : base(id)
        {
        }

       
        private int m_loginService = -1;
        private int m_agentMgrId = -1;

        public override void OnInit()
        {
            base.OnInit();

            
            m_loginService = SimpleGameServer.Instance.FindSingletonServiceByName("Login");
            m_agentMgrId = SimpleGameServer.Instance.FindSingletonServiceByName("AgentMgr");

            RegisterServiceMsgNtfHandler<GatewaySendPackageNtf>(HandleSendPackage);
            RegisterServiceMsgNtfHandler<GatewaySendPackageListNtf>(HandleSendPackageList);
            RegisterServiceMsgNtfHandler<GatewaySendPackage2Ntf>(HandleSendPackage);
            RegisterServiceMsgNtfHandler<GatewaySendPackageList2Ntf>(HandleSendPackageList);
        }

        protected override async Task OnClientConnect(SocketIndentifier s)
        {
            await base.OnClientConnect(s); 
        }

        protected override async Task OnClientDisconnect(SocketIndentifier s, string reason)
        {
            await base.OnClientDisconnect(s, reason);

            Send(m_loginService, new LogoutNtf()
            {
                GatewayId = this.m_id,
                Socket = s,
            });
            //s.Dispose();
            await Task.CompletedTask;
        }

        #region 客户端消息处理

        protected virtual async Task DispatchClientMsg(SocketIndentifier s, IMessage msg)
        {
            //SunNet.Instance.Log.Info(string.Format("GateWay:HandleClientMsg {0}", msg.ToString()));
            var msgId = SimpleGameServer.Instance.ProtocolDic.GetIdByType(msg.GetType());
            if (msgId == SunDonetProtocolDictionary.MsgId_LoginReq)
            {

                await DispatchLoginReq(s, msg as LoginReq);
            }
            else if (msgId == SunDonetProtocolDictionary.MsgId_CreateAccountReq)
            {
                await DispatchCreateReq(s, msg as CreateAccountReq);
            }
            else
            {
                var searchAck = await Call<AgentSearchReq, AgentSearchAck>(m_agentMgrId, new AgentSearchReq()
                {
                    Socket = s,
                });
                if (searchAck.Result == ErrorCode.OK && searchAck.RegisterItem != null)
                {
                    var agentId = searchAck.RegisterItem.AgentId;
                    Send(agentId, new ClientMsgHandleNtf()
                    {
                        m_req = msg,
                    });
                }
            }
        }

        private async Task DispatchLoginReq(SocketIndentifier s, LoginReq req)
        {
            SimpleGameServer.Instance.Log.Info(string.Format("GateWay:HandleLoginReq {0}", req.ToString()));
            Send(m_loginService, new LoginNtf()
            {
                GatewayId = this.m_id,
                Socket = s,
                Req = req,
            });
        }

        private async Task DispatchCreateReq(SocketIndentifier s, CreateAccountReq req)
        {
            SimpleGameServer.Instance.Log.Info(string.Format("GateWay:HandleCreateReq {0}", req.ToString()));
            Send(m_loginService, new CreateAccountNtf()
            {
                GatewayId = this.m_id,
                Socket = s,
                Req = req,
            });
        }

        #endregion

        #region 服务间消息处理
        private async Task HandleSendPackage(GatewaySendPackageNtf ntf)
        {
            //SunNet.Instance.Log.Info(string.Format("Gateway:HandleSendPackage {0}", ntf));
            var agentId = ntf.AgentId;
            var searchAck = await Call<AgentSearchReq, AgentSearchAck>(m_agentMgrId, new AgentSearchReq()
            {
                AgentId = agentId,
            });
            var socket = searchAck.RegisterItem.Socket;
            await SendPackageIml(socket, ntf.Msg);
        }

        private async Task HandleSendPackageList(GatewaySendPackageListNtf ntf)
        {
            //SunNet.Instance.Log.Info(string.Format("Gateway:HandleSendPackageList {0}", ntf));
            var agentId = ntf.AgentId;
            var searchAck = await Call<AgentSearchReq, AgentSearchAck>(m_agentMgrId, new AgentSearchReq()
            {
                AgentId = agentId,
            });
            var socket = searchAck.RegisterItem.Socket;
            await SendPackageListImpl(socket, ntf.MsgList);
        }

        private async Task HandleSendPackage(GatewaySendPackage2Ntf ntf)
        {
            //SunNet.Instance.Log.Info(string.Format("Gateway:HandleSendPackage {0}", ntf));
            var socket = ntf.Socket;
            await SendPackageIml(socket, ntf.Msg);
        }

        private async Task HandleSendPackageList(GatewaySendPackageList2Ntf ntf)
        {
            //SunNet.Instance.Log.Info(string.Format("Gateway:HandleSendPackageList {0}", ntf));
            var socket = ntf.Socket;
            await SendPackageListImpl(socket, ntf.MsgList);
        }
        #endregion

        public static void SendPackage(int gateway, int agentId, IMessage msg)
        {
            SimpleGameServer.Instance.Send(gateway, new GatewaySendPackageNtf()
            {
                Msg = msg,
                AgentId = agentId,
            });
        }

        public static void SendPackageList(int gateway, int agentId, List<IMessage> msgList)
        {
            SimpleGameServer.Instance.Send(gateway, new GatewaySendPackageListNtf()
            {
                MsgList = msgList,
                AgentId = agentId,
            });
        }

        public static void SendPackage(int gateway, SocketIndentifier socket, IMessage msg)
        {
            SimpleGameServer.Instance.Send(gateway, new GatewaySendPackage2Ntf()
            {
                Msg = msg,
                Socket = socket,
            });
        }

        public static void SendPackageList(int gateway, SocketIndentifier socket, List<IMessage> msgList)
        {
            SimpleGameServer.Instance.Send(gateway, new GatewaySendPackageList2Ntf()
            {
                MsgList = msgList,
                Socket = socket,
            });
        }
    }
}
