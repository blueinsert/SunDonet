using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class S2SGatewaySendPackageNtf : ServiceMsgNtf
    {
        public int AgentId;
        public IMessage Msg;

        public override string ToString()
        {
            return string.Format("AgentId:{0} Msg:{1}", this.AgentId, this.Msg);
        }
    }

    public class S2SGatewaySendPackageListNtf : ServiceMsgNtf
    {
        public int AgentId;
        public List<IMessage> MsgList;

        public override string ToString()
        {
            return string.Format("AgentId:{0} Msgs:{1}", this.AgentId, this.MsgList);
        }
    }

    public class S2SGatewaySendPackage2Ntf : ServiceMsgNtf
    {
        public SocketIndentifier Socket;
        public IMessage Msg;

        public override string ToString()
        {
            return string.Format("Socket:{0} Msg:{1}", this.Socket.ToString(), this.Msg);
        }
    }

    public class S2SGatewaySendPackageList2Ntf : ServiceMsgNtf
    {
        public SocketIndentifier Socket;
        public List<IMessage> MsgList;

        public override string ToString()
        {
            return string.Format("Socket:{0} Msg:{1}", this.Socket.ToString(), this.MsgList);
        }
    }
}
