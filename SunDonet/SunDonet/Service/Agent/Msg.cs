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
        public string m_userId;
    }

    public class S2SAgentInitAck : ServiceMsgAck
    {
        public int res;
    }

    public class S2SClientMsgHandleReq : ServiceMsgReq
    {
        public IMessage m_req;
    }

    public class S2SClientMsgHandleAck : ServiceMsgAck
    {
        public IMessage m_ack;
    }
}
