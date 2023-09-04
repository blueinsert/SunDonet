using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
   public class LoginReq : LocalAwaitableServiceMsgReq
    {
        public string m_name;
        public string m_password;
    }

    public class LoginAck : LocalAwaitableServiceMsgAck
    {
        public int m_res;
    }
}
