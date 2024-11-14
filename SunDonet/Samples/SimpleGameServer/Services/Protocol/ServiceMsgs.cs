using SunDonet.Protocol;
using SunDonet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace bluebean.SimpleGameServer.ServiceProtocol
{
    public class GatewaySendPackageNtf : ServiceMsgNtf
    {
        public int AgentId;
        public IMessage Msg;

        public override string ToString()
        {
            return string.Format("AgentId:{0} Msg:{1}", this.AgentId, this.Msg);
        }
    }

    public class GatewaySendPackageListNtf : ServiceMsgNtf
    {
        public int AgentId;
        public List<IMessage> MsgList;

        public override string ToString()
        {
            return string.Format("AgentId:{0} Msgs:{1}", this.AgentId, this.MsgList);
        }
    }

    public class GatewaySendPackage2Ntf : ServiceMsgNtf
    {
        public SocketIndentifier Socket;
        public IMessage Msg;

        public override string ToString()
        {
            return string.Format("Socket:{0} Msg:{1}", this.Socket.ToString(), this.Msg);
        }
    }

    public class GatewaySendPackageList2Ntf : ServiceMsgNtf
    {
        public SocketIndentifier Socket;
        public List<IMessage> MsgList;

        public override string ToString()
        {
            return string.Format("Socket:{0} Msg:{1}", this.Socket.ToString(), this.MsgList);
        }
    }

    public class LoginNtf : ServiceMsgNtf
    {
        public int GatewayId;
        public SocketIndentifier Socket;
        public LoginReq Req;
    }


    public class CreateAccountNtf : ServiceMsgNtf
    {
        public int GatewayId;
        public SocketIndentifier Socket;
        public CreateAccountReq Req;
    }

    public class LogoutNtf : ServiceMsgNtf
    {
        public int GatewayId;
        public SocketIndentifier Socket;
    }

    public class AgentSearchReq : ServiceMsgReq
    {
        public string UserId;
        public int AgentId = -1;
        public SocketIndentifier Socket = null;

        public override string ToString()
        {
            return string.Format("UserId:{0} AgentId:{1} Socket:{2}", this.UserId != null ? this.UserId : "", this.AgentId, this.Socket != null ? this.Socket.ToString() : "");
        }
    }

    public class AgentSearchAck : ServiceMsgAck
    {
        public int Result;
        public AgentRegisterItem RegisterItem = null;
    }

    public class AgentRegisterReq : ServiceMsgReq
    {
        public string UserId;
        public int AgentId;
        public int GatewayId;
        public SocketIndentifier Socket;

        public override string ToString()
        {
            return string.Format("UserId:{0} AgentId:{1} GatewayId:{2} Socket:{3}", this.UserId, this.AgentId, this.GatewayId, this.Socket.ToString());
        }
    }

    public class AgentRegisterAck : ServiceMsgAck
    {
        public int Result;
    }

    public class AgentRemoveReq : ServiceMsgReq
    {
        public int AgentId;

        public override string ToString()
        {
            return string.Format("AgentId:{0}", this.AgentId);
        }
    }

    public class AgentRemoveAck : ServiceMsgAck
    {
        public int Result;
    }

    public class AgentInitReq : ServiceMsgReq
    {
        public string UserId;
        public int GatewayId;
    }

    public class AgentInitAck : ServiceMsgAck
    {
        public int res;
    }

    public class ClientMsgHandleNtf : ServiceMsgNtf
    {
        public IMessage m_req;
    }

    public class AgentExitReq : ServiceMsgReq
    {

    }

    public class AgentExitAck : ServiceMsgAck
    {

    }
}
