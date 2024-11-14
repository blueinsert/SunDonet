using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public enum EncodeProtocol
    {
        Protobuf,
        Json,
        Text,
    }

    public class S2SEncodeReq : ServiceMsgReq
    {
        public object DataObj;
        public EncodeProtocol ProtocolType;
    }

    public class S2SEncodeAck : ServiceMsgAck
    {
        public ClientBuffer Buffer;
    }

    public class S2SDecodeReq : ServiceMsgReq
    {
        public EncodeProtocol ProtocolType;
        public ClientBuffer Buffer;
    }

    public class S2SDecodeAck : ServiceMsgAck
    {
        public object DataObj;
        public int ByteLenHandled;
    }
}
