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

    public class EncodeReq : ServiceMsgReq
    {
        public object m_dataObj;
        public EncodeProtocol m_protocolType;
    }

    public class EncodeAck : ServiceMsgAck
    {
        public byte[] m_data;
        public int m_dataLen;
    }

    public class DecodeReq : ServiceMsgReq
    {
        public EncodeProtocol m_protocolType;
        public byte[] m_data;
        public int m_dataLen;
    }

    public class DecodeAck : ServiceMsgAck
    {
        public object m_dataObj;
        public int m_byteHandled;
    }
}
