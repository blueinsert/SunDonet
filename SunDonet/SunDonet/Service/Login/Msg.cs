using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
   public class S2SLoginNtf : ServiceMsgNtf
    {
        public int m_gatewayId;
        public string m_name;
        public string m_password;
    }


    public class S2SCreateAccountNtf : ServiceMsgNtf
    {
        public int m_gatewayId;
        public string m_name;
        public string m_password;
    }

}
