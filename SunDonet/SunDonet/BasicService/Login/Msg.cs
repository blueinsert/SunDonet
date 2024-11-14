using SunDonet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
   public class S2SLoginNtf : ServiceMsgNtf
    {
        public int GatewayId;
        public SocketIndentifier Socket;
        public LoginReq Req;
    }


    public class S2SCreateAccountNtf : ServiceMsgNtf
    {
        public int GatewayId;
        public SocketIndentifier Socket;
        public CreateAccountReq Req;
    }

    public class S2SLogoutNtf : ServiceMsgNtf
    {
        public int GatewayId;
        public SocketIndentifier Socket;
    }
}
