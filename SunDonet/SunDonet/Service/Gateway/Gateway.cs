using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using SunDonet.Protocol;

namespace SunDonet
{

    public class Gateway : ServiceBase
    {
        public Gateway(int id) : base(id)
        {

        }

        private int m_encoderService = -1;
        private int m_loginService = -1;

        private Dictionary<Socket, ClientBuffer> m_clientBuffDic = new Dictionary<Socket, ClientBuffer>();
        //socket - agent id
        private Dictionary<Socket, int> m_players = new Dictionary<Socket, int>();

        public override void OnInit()
        {
            base.OnInit();
            m_encoderService = SunNet.Instance.FindSingletonServiceByName("Encoder");
            m_loginService = SunNet.Instance.FindSingletonServiceByName("Login");

            SunNet.Instance.Listen(8888,this.m_id);

            //RegisterMsgCallHandler<LoginReq, LoginAck>(HandleLoginReq);
        }

        public override async Task OnClientConnect(Socket s)
        {
            Console.WriteLine("Gateway:OnClientConnect " + s.RemoteEndPoint.ToString());
            m_clientBuffDic.Add(s, ClientBuffer.GetBuffer(8 * 1024 * 5));
        }

        public override async Task OnClientDisconnect(Socket s)
        {
            Console.WriteLine("Gateway:OnClientDisconnect " + s.RemoteEndPoint.ToString());
            m_clientBuffDic.Remove(s);
        }

        public override async Task OnClientData(Socket s, ClientBuffer buff)
        {
            //Console.WriteLine("Gateway:OnClientData len:" + buff.m_dataLen);
            var sumBuff = m_clientBuffDic[s];
            if(sumBuff.m_dataLen + buff.m_dataLen > sumBuff.m_buffer.Length)
            {
                Console.WriteLine("Gateway:OnClientData sumBuff need resize");
                //todo
            }
            Array.Copy(buff.m_buffer, 0, sumBuff.m_buffer, sumBuff.m_dataLen, buff.m_dataLen);
            sumBuff.m_dataLen += buff.m_dataLen;
            ClientBuffer.BackBuffer(buff);
            bool doNext = false;
            do
            {
                S2SDecodeReq req = new S2SDecodeReq()
                {
                    m_protocolType = EncodeProtocol.Protobuf,
                    m_buff = sumBuff,
                };
                S2SDecodeAck ack = await SunNet.Instance.Call<S2SDecodeReq, S2SDecodeAck>(m_encoderService, req);
                doNext = false;
                if (ack != null && ack.m_byteHandled != 0 && ack.m_dataObj != null)
                {
                    await HandleClientMsg(s, ack.m_dataObj as IMessage);
                    Array.Copy(sumBuff.m_buffer, ack.m_byteHandled, sumBuff.m_buffer, 0, sumBuff.m_dataLen - ack.m_byteHandled);
                    sumBuff.m_dataLen -= ack.m_byteHandled;
                    if(sumBuff.m_dataLen > Encoder.ProtocolHeaderLen)
                    {
                        doNext = true;
                    }
                }
                else
                {
                    Console.WriteLine("Gateway:OnClientData decode failed or ignore");
                }
            } while (doNext);
            
        }

        private async Task SendPackageIml(Socket s, IMessage msg)
        {
            if (msg == null)
                return;
            S2SEncodeReq req = new S2SEncodeReq()
            {
                m_dataObj = msg,
                m_protocolType = EncodeProtocol.Protobuf,
            };
            var encodeAck = await SunNet.Instance.Call<S2SEncodeReq, S2SEncodeAck>(m_encoderService, req);
            SunNet.Instance.Send(s, encodeAck.m_buffer);
        }

        private async Task SendPackageList(Socket s, List<IMessage> msgList)
        {
            if (msgList.Count == 0)
                return;
            foreach(var msg in msgList)
            {
                if (msg != null)
                {
                    await SendPackageIml(s, msg);
                }
            }
            
        }

        private async Task HandleClientMsg(Socket s, IMessage msg)
        {
            //Console.WriteLine(string.Format("GateWay:HandleClientMsg {0}", msg.ToString()));
            var msgId = SunNet.Instance.ProtocolDic.GetIdByType(msg.GetType());
            IMessage ack = null;
            if(msgId == SunDonetProtocolDictionary.MsgId_LoginReq)
            {
                ack = await HandleLoginReq(s, msg as LoginReq);
                await SendPackageIml(s, ack);
            }
            else if(msgId == SunDonetProtocolDictionary.MsgId_CreateAccountReq)
            {
                ack = await HandleCreateReq(s,msg as CreateAccountReq);
                await SendPackageIml(s, ack);
            }
            else
            {
                if (m_players.ContainsKey(s))
                {
                    int agentId = m_players[s];
                    var handleAck = await SunNet.Instance.Call<S2SClientMsgHandleReq, S2SClientMsgHandleAck>(agentId, new S2SClientMsgHandleReq()
                    {
                        m_req = msg,
                    });
                    if(handleAck.m_acks!=null && handleAck.m_acks.Count != 0)
                    {
                        await SendPackageList(s, handleAck.m_acks);
                    }else if (handleAck.m_ack != null)
                    {
                        await SendPackageIml(s, handleAck.m_ack);
                    }
                }
            }
        }

        private async Task<LoginAck> HandleLoginReq(Socket s, LoginReq req)
        {
            Console.WriteLine(string.Format("GateWay:HandleLoginReq {0}", req.ToString()));
            var loginAck = await SunNet.Instance.Call<S2SLoginReq, S2SLoginAck>(m_loginService, new S2SLoginReq()
            {
                m_name = req.UserName,
                m_password = req.UserPassword,
            });
            LoginAck ack = new LoginAck()
            {
                Result = loginAck.m_res,
            };
            if(ack.Result == ErrorCode.OK)
            {
                var agentId = SunNet.Instance.NewService("Agent");
                m_players.Add(s, agentId);
                await SunNet.Instance.Call<S2SAgentInitReq, S2SAgentInitAck>(agentId, new S2SAgentInitReq() {
                    m_userId = req.UserName,
                });
            }
            Console.WriteLine(string.Format("GateWay:HandleLoginReq ack:{0}", ack.ToString()));
            return ack;
        }

        private async Task<CreateAccountAck> HandleCreateReq(Socket s, CreateAccountReq req)
        {
            Console.WriteLine(string.Format("GateWay:HandleCreateReq {0}", req.ToString()));
            var createAccountAck = await SunNet.Instance.Call<S2SCreateAccountReq, S2SCreateAccountAck>(m_loginService, new S2SCreateAccountReq()
            {
                m_name = req.UserName,
                m_password = req.UserPassword,
            });
            CreateAccountAck ack = new CreateAccountAck()
            {
                Result = createAccountAck.m_res,
            };
            return ack;
        }
    }
}
