using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SunDonet;
using SunDonet.Protocol;
using Google.Protobuf;
using NetLibClient;
using System.Threading;

namespace TestClient
{
    class Program
    {
        static TAck Send<TReq,TAck>(TcpClient client,TReq req) where TAck: class, IMessage where TReq:class, IMessage
        {
            ProtocolDictionaryBase m_protocolDictionary = new SunDonetProtocolDictionary();
            var ack = SunDonet.Encoder.EncodeGoogleProtobuf(req, m_protocolDictionary);
            client.GetStream().Write(ack.Buffer.m_buffer, 0, ack.Buffer.m_dataLen);
            byte[] buff = new byte[8 * 1024 * 5];
            int received = client.GetStream().Read(buff, 0, buff.Length);
            var decodeAck = SunDonet.Encoder.DecodeGoogleProtobuf(buff, received, m_protocolDictionary);
            var res = decodeAck.DataObj as TAck;
            return res;
        }

        static void Main(string[] args)
        {
           
            var userId = "zzx003";
            var password = "123";
            PlayerContext playerContext = new PlayerContext();
            Task.Run(() => { Proc_TickClient(playerContext); });
            playerContext.EventOnLoginAck += (res) => {
                if(res == ErrorCode.OK)
                {
                    playerContext.SendPlayerInfoInitReq();
                }
                if (res == ErrorCode.LoginAccountNotExist)
                {
                    playerContext.SendCreateAccountReq(userId, password);
                }
            };
            playerContext.EventOnCreateAccountAck += (res) => {
            };
            playerContext.Connect("127.0.0.1", 8888);
            playerContext.SendLoginReq(userId, password);

            /*
            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", 8888);
             var req = new SunDonet.Protocol.LoginReq()
            {
                UserName = userId,
                UserPassword = password,
            };
            SunNet.Instance.Log.Info(string.Format("loginReq:{0}", req));
            var ack = Send<SunDonet.Protocol.LoginReq, SunDonet.Protocol.LoginAck>(client,req);
            SunNet.Instance.Log.Info(string.Format("loginAck:{0}", ack));
           if(ack.Result == ErrorCode.LoginAccountNotExist)
            {
                SunNet.Instance.Log.Info(string.Format("send create Req"));
                var createAck = Send<SunDonet.Protocol.CreateAccountReq, SunDonet.Protocol.CreateAccountAck>(client, new CreateAccountReq() {
                    UserName = userId,
                    UserPassword = password,
                });
                SunNet.Instance.Log.Info(string.Format("createAck:{0}", createAck));
                ack = Send<SunDonet.Protocol.LoginReq, SunDonet.Protocol.LoginAck>(client, req);
                SunNet.Instance.Log.Info(string.Format("loginAck:{0}", ack));
            }
            */
            var exitEvent = new System.Threading.ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();

            //client.Close();
        }

        private static void Proc_TickClient(PlayerContext playerContext)
        {
            while (true)
            {
                playerContext.Tick();
                Thread.Sleep(300);
            }
        }
    }
}
