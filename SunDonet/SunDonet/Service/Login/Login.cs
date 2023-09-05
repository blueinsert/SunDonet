using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SunDonet
{

    public class Login : ServiceBase
    {
        public Login(int id) : base(id)
        {

        }

        public override void OnInit()
        {
            base.OnInit();
        }

        public override async Task<ServiceMsgAck> OnServiceCall(ServiceMsgReq req)
        {
            if(req is LoginReq)
            {
                return await HandleLoginReq(req as LoginReq);
            }
            return null;
        }

        public async Task<LoginAck> HandleLoginReq(LoginReq req)
        {
            Console.WriteLine("LoginService:HandleLoginReq");
            LoginAck ack = new LoginAck() { m_res = 0 };
            //Thread.Sleep(3000);
            return ack;
        }
    }
}
