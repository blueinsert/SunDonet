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
    /// <summary>
    /// 网关服务，负责发包和收包分发
    /// </summary>
    public class Gateway : ServiceBase
    {
        public Gateway(int id) : base(id)
        {

        }

        private int m_encoderService = -1;
        private int m_loginService = -1;

        private Dictionary<Socket, ClientBuffer> m_clientBuffDic = new Dictionary<Socket, ClientBuffer>();

        public override void OnInit()
        {
            base.OnInit();
            m_encoderService = SunNet.Instance.FindSingletonServiceByName("Encoder");
            m_loginService = SunNet.Instance.FindSingletonServiceByName("Login");

            SunNet.Instance.Listen(8888,this.m_id);

            RegisterServiceMsgNtfHandler<S2SGatewaySendPackageNtf>(HandleSendPackage);
            RegisterServiceMsgNtfHandler<S2SGatewaySendPackageListNtf>(HandleSendPackageList);
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
                    await DispatchClientMsg(s, ack.m_dataObj as IMessage);
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

        private async Task DispatchClientMsg(Socket s, IMessage msg)
        {
            //Console.WriteLine(string.Format("GateWay:HandleClientMsg {0}", msg.ToString()));
            var msgId = SunNet.Instance.ProtocolDic.GetIdByType(msg.GetType());
            if(msgId == SunDonetProtocolDictionary.MsgId_LoginReq)
            {
                
                await DispatchLoginReq(s, msg as LoginReq);
            }
            else if(msgId == SunDonetProtocolDictionary.MsgId_CreateAccountReq)
            {
                await DispatchCreateReq(s,msg as CreateAccountReq);
            }
            else
            {
                if (m_players.ContainsKey(s))
                {
                    int agentId = m_players[s];
                    SunNet.Instance.Send(agentId, new S2SClientMsgHandleNtf()
                    {
                        m_req = msg,
                    });
                }
            }
        }

        private async Task<LoginAck> DispatchLoginReq(Socket s, LoginReq req)
        {
            Console.WriteLine(string.Format("GateWay:HandleLoginReq {0}", req.ToString()));
            SunNet.Instance.Send(m_loginService, new S2SLoginNtf()
            {
                m_name = req.UserName,
                m_password = req.UserPassword,
            });

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

        private async Task<CreateAccountAck> DispatchCreateReq(Socket s, CreateAccountReq req)
        {
            Console.WriteLine(string.Format("GateWay:HandleCreateReq {0}", req.ToString()));
            SunNet.Instance.Send(m_loginService, new S2SCreateAccountNtf()
            {
                m_name = req.UserName,
                m_password = req.UserPassword,
            });
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

        private async Task HandleSendPackage(S2SGatewaySendPackageNtf ntf)
        {
            var socket = m_player2SocketDic[ntf.AgentId];
            SendPackageIml(socket, ntf.Msg);
        }

        private async Task HandleSendPackageList(S2SGatewaySendPackageListNtf ntf)
        {
            var socket = m_player2SocketDic[ntf.AgentId];
            SendPackageList(socket, ntf.MsgList);
        }
    }
}
