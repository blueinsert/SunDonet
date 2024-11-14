using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace SunDonet
{
  public class S2SAgentSearchReq : ServiceMsgReq
    {
        public string UserId;
        public int AgentId = -1;
        public SocketIndentifier Socket = null;

        public override string ToString()
        {
            return string.Format("UserId:{0} AgentId:{1} Socket:{2}", this.UserId != null ? this.UserId : "", this.AgentId, this.Socket != null ? this.Socket.ToString() : "");
        }
    }

    public class S2SAgentSearchAck : ServiceMsgAck
    {
        public int Result;
        public AgentRegisterItem RegisterItem = null;
    }

    public class S2SAgentRegisterReq : ServiceMsgReq
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

    public class S2SAgentRegisterAck : ServiceMsgAck
    {
        public int Result;
    }

    public class S2SAgentRemoveReq : ServiceMsgReq
    {
        public int AgentId;

        public override string ToString()
        {
            return string.Format("AgentId:{0}", this.AgentId);
        }
    }

    public class S2SAgentRemoveAck : ServiceMsgAck
    {
        public int Result;
    }
}
