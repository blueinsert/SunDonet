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

        private int m_encoderService = -1;

        public override void OnInit()
        {
            base.OnInit();
            m_encoderService = SunNet.Instance.NewService("Encoder");
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
            DecodeReq req = new DecodeReq()
            {
                m_protocolType = EncodeProtocol.Protobuf,
                m_data = data,
            };
            DecodeAck ack = await SunNet.Instance.Call<DecodeReq, DecodeAck>(m_encoderService, req);
            if (ack.m_byteHandled != 0)
            {
                if (ack.m_dataObj != null)
                {
                    Console.WriteLine("Gateway:OnClientData " + ack.m_dataObj.GetType());
                }
            }
            else
            {
                //
            }
        }
    }
}
