using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SunDonet.DB;

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
            RegisterServiceMsgCallHandler<S2SLoginReq, S2SLoginAck>(HandleLoginReq);
            RegisterServiceMsgCallHandler<S2SCreateAccountReq, S2SCreateAccountAck>(HandleCreateAccountReq);
        }

        private async Task<ServiceMsgAck> HandleLoginReq_(ServiceMsgReq req)
        {
            return await HandleLoginReq(req as S2SLoginReq);
        }

        private async Task<S2SLoginAck> HandleLoginReq(S2SLoginReq req)
        {
            //Console.WriteLine("LoginService:HandleLoginReq");

            S2SLoginAck ack = new S2SLoginAck() { m_res = 0 };

            var dbAccount = await DBMethod.GetAccount(req.m_name);
            if(dbAccount == null)
            {
                ack.m_res = ErrorCode.LoginAccountNotExist;
            }
            else
            {
                if(dbAccount.Password != req.m_password)
                {
                    ack.m_res = ErrorCode.LoginPasswordError;
                }
            }
            return ack;
        }

        private async Task<S2SCreateAccountAck> HandleCreateAccountReq(S2SCreateAccountReq req)
        {
            Console.WriteLine("LoginService:HandleCreateAccountReq");

            S2SCreateAccountAck ack = new S2SCreateAccountAck() { m_res = 0 };

            var res = await DBMethod.CreateAccount(req.m_name, req.m_password);

            ack.m_res = 0;
            return ack;
        }
    }
}
