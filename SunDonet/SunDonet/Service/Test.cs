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

        public override void OnClientConnect(Socket s)
        {
            Console.WriteLine("Test:OnClientConnect id:" + m_id);
            base.OnClientConnect(s);
        }

        public override void OnClientDisconnect(Socket s)
        {
            Console.WriteLine("Test:OnClientDisconnect id:" + m_id);
            base.OnClientDisconnect(s);
        }

        public override void OnClientData(Socket s, byte[] data)
        {
            Console.WriteLine(string.Format("Test:OnClientData id:{0} msg:{1}",m_id, System.Text.Encoding.UTF8.GetString(data)));
            base.OnClientData(s, data);
        }

        public override void OnServiceMsg(ServiceMsg msg)
        {
            Console.WriteLine("Test:OnServiceMsg id:" + m_id);
            base.OnServiceMsg(msg);
        }
    }

}
