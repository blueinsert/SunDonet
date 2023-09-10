﻿using System;
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
        private int m_agentMgrId = -1;

        private Dictionary<Socket, ClientBuffer> m_clientBuffDic = new Dictionary<Socket, ClientBuffer>();

        public override void OnInit()
        {
            base.OnInit();
            m_encoderService = SunNet.Instance.FindSingletonServiceByName("Encoder");
            m_loginService = SunNet.Instance.FindSingletonServiceByName("Login");
            m_agentMgrId = SunNet.Instance.FindSingletonServiceByName("AgentMgr");

            SunNet.Instance.Listen(8888,this.m_id);

            RegisterServiceMsgNtfHandler<S2SGatewaySendPackageNtf>(HandleSendPackage);
            RegisterServiceMsgNtfHandler<S2SGatewaySendPackageListNtf>(HandleSendPackageList);
            RegisterServiceMsgNtfHandler<S2SGatewaySendPackage2Ntf>(HandleSendPackage);
            RegisterServiceMsgNtfHandler<S2SGatewaySendPackageList2Ntf>(HandleSendPackageList);
        }

        public override async Task OnClientConnect(Socket s)
        {
            SunNet.Instance.Log.Info("Gateway:OnClientConnect " + s.RemoteEndPoint.ToString());
            m_clientBuffDic.Add(s, ClientBuffer.GetBuffer(8 * 1024 * 5));
        }

        public override async Task OnClientDisconnect(Socket s)
        {
            SunNet.Instance.Log.Info("Gateway:OnClientDisconnect " + s.RemoteEndPoint.ToString());
            m_clientBuffDic.Remove(s);
        }

        #region 收包与分发

        public override async Task OnClientData(Socket s, ClientBuffer buff)
        {
            //SunNet.Instance.Log.Info("Gateway:OnClientData len:" + buff.m_dataLen);
            var sumBuff = m_clientBuffDic[s];
            if(sumBuff.m_dataLen + buff.m_dataLen > sumBuff.m_buffer.Length)
            {
                SunNet.Instance.Log.Info("Gateway:OnClientData sumBuff need resize");
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
                    ProtocolType = EncodeProtocol.Protobuf,
                    Buffer = sumBuff,
                };
                S2SDecodeAck ack = await SunNet.Instance.Call<S2SDecodeReq, S2SDecodeAck>(m_encoderService, req);
                doNext = false;
                if (ack != null && ack.ByteLenHandled != 0 && ack.DataObj != null)
                {
                    await DispatchClientMsg(s, ack.DataObj as IMessage);
                    Array.Copy(sumBuff.m_buffer, ack.ByteLenHandled, sumBuff.m_buffer, 0, sumBuff.m_dataLen - ack.ByteLenHandled);
                    sumBuff.m_dataLen -= ack.ByteLenHandled;
                    if(sumBuff.m_dataLen > GoogleProtobufHelper.ProtocolHeaderLen)
                    {
                        doNext = true;
                    }
                }
                else
                {
                    SunNet.Instance.Log.Info("Gateway:OnClientData decode failed or ignore");
                }
            } while (doNext);
            
        }

        private async Task DispatchClientMsg(Socket s, IMessage msg)
        {
            //SunNet.Instance.Log.Info(string.Format("GateWay:HandleClientMsg {0}", msg.ToString()));
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
                var searchAck = await SunNet.Instance.Call<S2SAgentSearchReq, S2SAgentSearchAck>(m_agentMgrId, new S2SAgentSearchReq()
                {
                    Socket = s,
                });
                if(searchAck.Result == ErrorCode.OK && searchAck.RegisterItem != null)
                {
                    var agentId = searchAck.RegisterItem.AgentId;
                    SunNet.Instance.Send(agentId, new S2SClientMsgHandleNtf()
                    {
                        m_req = msg,
                    });
                }
            }
        }

        private async Task DispatchLoginReq(Socket s, LoginReq req)
        {
            SunNet.Instance.Log.Info(string.Format("GateWay:HandleLoginReq {0}", req.ToString()));
            SunNet.Instance.Send(m_loginService, new S2SLoginNtf()
            {
                m_gatewayId = this.m_id,
                m_socket = s,
                m_req = req,
            });
        }

        private async Task DispatchCreateReq(Socket s, CreateAccountReq req)
        {
            SunNet.Instance.Log.Info(string.Format("GateWay:HandleCreateReq {0}", req.ToString()));
            SunNet.Instance.Send(m_loginService, new S2SCreateAccountNtf()
            {
                m_gatewayId = this.m_id,
                m_socket = s,
                m_req = req,
            });
        }

        #endregion

        #region 发包

        private async Task SendPackageIml(Socket s, IMessage msg)
        {
            if (msg == null)
                return;
            SunNet.Instance.Log.Info(string.Format("Gateway:SendPackageIml remote:{0} {1} {2}", s.RemoteEndPoint.ToString(), msg.GetType(), msg));
            S2SEncodeReq req = new S2SEncodeReq()
            {
                DataObj = msg,
                ProtocolType = EncodeProtocol.Protobuf,
            };
            var encodeAck = await SunNet.Instance.Call<S2SEncodeReq, S2SEncodeAck>(m_encoderService, req);
            SunNet.Instance.Send(s, encodeAck.Buffer);
        }

        private async Task SendPackageList(Socket s, List<IMessage> msgList)
        {
            if (msgList.Count == 0)
                return;
            foreach (var msg in msgList)
            {
                if (msg != null)
                {
                    await SendPackageIml(s, msg);
                }
            }

        }

        public static void SendPackage(int gateway, int agentId, IMessage msg)
        {
            SunNet.Instance.Send(gateway, new S2SGatewaySendPackageNtf()
            {
                Msg = msg,
                AgentId = agentId,
            });
        }

        public static void SendPackageList(int gateway, int agentId, List<IMessage> msgList)
        {
            SunNet.Instance.Send(gateway, new S2SGatewaySendPackageListNtf()
            {
                MsgList = msgList,
                AgentId = agentId,
            });
        }

        public static void SendPackage(int gateway, Socket socket, IMessage msg)
        {
            SunNet.Instance.Send(gateway, new S2SGatewaySendPackage2Ntf()
            {
                Msg = msg,
                Socket = socket,
            });
        }

        public static void SendPackageList(int gateway, Socket socket, List<IMessage> msgList)
        {
            SunNet.Instance.Send(gateway, new S2SGatewaySendPackageList2Ntf()
            {
                MsgList = msgList,
                Socket = socket,
            });
        }

        private async Task HandleSendPackage(S2SGatewaySendPackageNtf ntf)
        {
            //SunNet.Instance.Log.Info(string.Format("Gateway:HandleSendPackage {0}", ntf));
            var agentId = ntf.AgentId;
            var searchAck = await SunNet.Instance.Call<S2SAgentSearchReq, S2SAgentSearchAck>(m_agentMgrId, new S2SAgentSearchReq()
            {
                AgentId = agentId,
            });
            var socket = searchAck.RegisterItem.Socket;
            await SendPackageIml(socket, ntf.Msg);
        }

        private async Task HandleSendPackageList(S2SGatewaySendPackageListNtf ntf)
        {
            //SunNet.Instance.Log.Info(string.Format("Gateway:HandleSendPackageList {0}", ntf));
            var agentId = ntf.AgentId;
            var searchAck = await SunNet.Instance.Call<S2SAgentSearchReq, S2SAgentSearchAck>(m_agentMgrId, new S2SAgentSearchReq()
            {
                AgentId = agentId,
            });
            var socket = searchAck.RegisterItem.Socket;
            await SendPackageList(socket, ntf.MsgList);
        }

        private async Task HandleSendPackage(S2SGatewaySendPackage2Ntf ntf)
        {
            //SunNet.Instance.Log.Info(string.Format("Gateway:HandleSendPackage {0}", ntf));
            var socket = ntf.Socket;
            await SendPackageIml(socket, ntf.Msg);
        }

        private async Task HandleSendPackageList(S2SGatewaySendPackageList2Ntf ntf)
        {
            //SunNet.Instance.Log.Info(string.Format("Gateway:HandleSendPackageList {0}", ntf));
            var socket = ntf.Socket;
             await SendPackageList(socket, ntf.MsgList);
        }
        #endregion
    }
}
