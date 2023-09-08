using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class S2SGatewaySendPackageNtf : ServiceMsgNtf
    {
        public int AgentId;
        public IMessage Msg;
    }

    public class S2SGatewaySendPackageListNtf : ServiceMsgNtf
    {
        public int AgentId;
        public List<IMessage> MsgList;
    }
}
