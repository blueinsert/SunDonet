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

        private Dictionary<Socket, ClientBuffer> m_clientBuffDic = new Dictionary<Socket, ClientBuffer>();

        public override void OnInit()
        {
            base.OnInit();
            m_encoderService = SunNet.Instance.NewService("Encoder");
            SunNet.Instance.Listen(8888,this.m_id);

        }

        public override async Task OnClientConnect(Socket s)
        {
            Console.WriteLine("Gateway:OnClientConnect id:" + m_id);
            m_clientBuffDic.Add(s, ClientBuffer.GetBuffer(8 * 1024 * 5));
        }

        public override async Task OnClientDisconnect(Socket s)
        {
            Console.WriteLine("Gateway:OnClientDisconnect id:" + m_id);
            m_clientBuffDic.Remove(s);
        }

        public override async Task OnClientData(Socket s, ClientBuffer buff)
        {
            var sumBuff = m_clientBuffDic[s];
            if(sumBuff.m_dataLen + buff.m_dataLen > sumBuff.m_buffer.Length)
            {
                Console.WriteLine("Gateway:OnClientData sumBuff need resize");
                //todo
            }
            Array.Copy(buff.m_buffer, 0, sumBuff.m_buffer, sumBuff.m_dataLen, buff.m_dataLen);
            sumBuff.m_dataLen += buff.m_dataLen;
            ClientBuffer.BackBuffer(buff);
            DecodeReq req = new DecodeReq()
            {
                m_protocolType = EncodeProtocol.Protobuf,
                m_buff = sumBuff,
            };
            DecodeAck ack = await SunNet.Instance.Call<DecodeReq, DecodeAck>(m_encoderService, req);
            if (ack!=null && ack.m_byteHandled != 0)
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
