using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace SunDonet
{
    public class S2SAgentInitReq : ServiceMsgReq
    {
        public string UserId;
        public int GatewayId;
    }

    public class S2SAgentInitAck : ServiceMsgAck
    {
        public int res;
    }

    public class S2SClientMsgHandleNtf : ServiceMsgNtf
    {
        public IMessage m_req;
    }

}
