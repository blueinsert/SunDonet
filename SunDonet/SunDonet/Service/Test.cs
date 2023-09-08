using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class Test:ServiceBase
    {
        public Test(int id) : base(id) { }

        public override void OnInit()
        {
            base.OnInit();
            Console.WriteLine("Test:OnInit id:"+m_id);
            SunNet.Instance.Listen(8888,m_id);
        }

        public override void OnExit()
        {
            Console.WriteLine("Test:OnExit id:" + m_id);
            base.OnExit();
        }

        public override async Task OnClientConnect(Socket s)
        {
            Console.WriteLine("Test:OnClientConnect id:" + m_id);
        }

        public override async Task OnClientDisconnect(Socket s)
        {
            Console.WriteLine("Test:OnClientDisconnect id:" + m_id);
        }

        public override async Task OnServiceMsg(ServiceMsgNtf msg)
        {
            Console.WriteLine("Test:OnServiceMsg id:" + m_id);
        }
    }

}
