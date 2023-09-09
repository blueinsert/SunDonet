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
        public int m_gatewayId;
        public Socket m_socket;
        public LoginReq m_req;
    }


    public class S2SCreateAccountNtf : ServiceMsgNtf
    {
        public int m_gatewayId;
        public Socket m_socket;
        public CreateAccountReq m_req;
    }

}
