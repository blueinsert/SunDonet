using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class Main : ServiceBase
    {
        public Main(int id) : base(id)
        {

        }

        public override void OnInit()
        {
            base.OnInit();
            SunNet.Instance.NewService("Gateway");
            SunNet.Instance.NewService("Login");
        }
    }
}
