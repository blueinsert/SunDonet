using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class Gateway : ServiceBase
    {
        public Gateway(int id) : base(id)
        {

        }

        public override void OnInit()
        {
            base.OnInit();
            SunNet.Instance.Listen(8888,this.m_id);
        }

        public override async Task OnClientConnect(Socket s)
        {
            Console.WriteLine("Gateway:OnClientConnect id:" + m_id);
        }

        public override async Task OnClientDisconnect(Socket s)
        {
            Console.WriteLine("Gateway:OnClientDisconnect id:" + m_id);
        }

        public override async Task OnClientData(Socket s, byte[] data)
        {
            var msg = System.Text.Encoding.UTF8.GetString(data);
            Console.WriteLine(string.Format("Gateway:OnClientData id:{0} msg:{1}", m_id, msg));
            if(msg == "login")
            {
                var sid = SunNet.Instance.FindSingletonServiceByName("Login");
                if (sid != -1)
                {
                    var req = new LoginReq() { };
                    var ack = await SunNet.Instance.SendLocalAwaitable<LoginReq,LoginAck>(sid, req);
                    if(ack != null && ack.m_res == 0)
                    {
                        Console.WriteLine("Gateway:LoginSuccess");
                    }
                    else
                    {
                        Console.WriteLine("Gateway:LoginFailed");
                    }
                }
            }
        }
    }
}
