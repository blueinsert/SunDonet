using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
   public class S2SLoginReq : ServiceMsgReq
    {
        public string m_name;
        public string m_password;
    }

    public class S2SLoginAck : ServiceMsgAck
    {
        public int m_res;
    }
}
