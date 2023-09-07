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
        public object m_dataObj;
        public EncodeProtocol m_protocolType;
    }

    public class S2SEncodeAck : ServiceMsgAck
    {
        public ClientBuffer m_buffer;
    }

    public class S2SDecodeReq : ServiceMsgReq
    {
        public EncodeProtocol m_protocolType;
        public ClientBuffer m_buff;
    }

    public class S2SDecodeAck : ServiceMsgAck
    {
        public object m_dataObj;
        public int m_byteHandled;
    }
}
