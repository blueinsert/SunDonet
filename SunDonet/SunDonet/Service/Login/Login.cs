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
            RegisterMsgCallHandler<S2SLoginReq, S2SLoginAck>(HandleLoginReq);
        }

        private async Task<ServiceMsgAck> HandleLoginReq_(ServiceMsgReq req)
        {
            return await HandleLoginReq(req as S2SLoginReq);
        }

        private async Task<S2SLoginAck> HandleLoginReq(S2SLoginReq req)
        {
            Console.WriteLine("LoginService:HandleLoginReq");
            S2SLoginAck ack = new S2SLoginAck() { m_res = 0 };
            //Thread.Sleep(3000);
            return ack;
        }
    }
}
