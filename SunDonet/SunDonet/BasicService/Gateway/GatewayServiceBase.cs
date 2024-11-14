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
    public abstract class GatewayServiceBase : ServiceBase
    {
        public GatewayServiceBase(int id) : base(id)
        {

        }

        private int m_encoderService = -1;

        private Dictionary<SocketIndentifier, ClientBuffer> m_clientBuffDic = new Dictionary<SocketIndentifier, ClientBuffer>();

        public override void OnInit()
        {
            base.OnInit();

            m_encoderService = SunNet.Instance.FindSingletonServiceByName("Encoder");

            var ip = GetStringParam("IP");
            var port = GetIntParam("Port");
            SunNet.Instance.Listen(port, this.m_id);
        }

        protected override async Task OnSocketMsg(MsgBase msg)
        {
            switch (msg.MessageType)
            {
                case MsgBase.MsgType.Socket_Accept:
                    await OnClientConnect((msg as SocketAcceptMsg).Client);
                    break;
                case MsgBase.MsgType.Socket_Disconnect:
                    var disconnectMsg = msg as SocketDisconnectMsg;
                    await OnClientDisconnect(disconnectMsg.ClientId, disconnectMsg.Reason);
                    break;
                case MsgBase.MsgType.Socket_Data:
                    var clientDataMsg = msg as SocketDataMsg;
                    await OnClientData(clientDataMsg.SocketId, clientDataMsg.Buff);
                    break;
            }
        }

        protected virtual async Task OnClientConnect(SocketIndentifier s)
        {
            SunNet.Instance.Log.Info("Gateway:OnClientConnect " + s.ToString());
            m_clientBuffDic.Add(s, ClientBuffer.GetBuffer(8 * 1024 * 5));
            await Task.CompletedTask;
        }

        protected virtual async Task OnClientDisconnect(SocketIndentifier s, string reason)
        {
            Debug.Log("Gateway:OnClientDisconnect {0} {1}", s.ToString(), reason);
            var clientBuff = m_clientBuffDic[s];
            m_clientBuffDic.Remove(s);
            ClientBuffer.BackBuffer(clientBuff);
            await Task.CompletedTask;
        }

        protected virtual async Task OnClientData(SocketIndentifier s, ClientBuffer buff)
        {
            //SunNet.Instance.Log.Info("Gateway:OnClientData len:" + buff.m_dataLen);
            var sumBuff = m_clientBuffDic[s];
            if (sumBuff.m_dataLen + buff.m_dataLen > sumBuff.m_buffer.Length)
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
                S2SDecodeAck ack = await Call<S2SDecodeReq, S2SDecodeAck>(m_encoderService, req);
                doNext = false;
                if (ack != null && ack.ByteLenHandled != 0 && ack.DataObj != null)
                {
                    await DispatchClientMsg(s, ack.DataObj as IMessage);
                    Array.Copy(sumBuff.m_buffer, ack.ByteLenHandled, sumBuff.m_buffer, 0, sumBuff.m_dataLen - ack.ByteLenHandled);
                    sumBuff.m_dataLen -= ack.ByteLenHandled;
                    if (sumBuff.m_dataLen > GoogleProtobufHelper.ProtocolHeaderLen)
                    {
                        doNext = true;
                    }
                }
                else
                {
                    SunNet.Instance.Log.Info("Gateway:OnClientData decode failed or ignore");
                }
            } while (doNext);

            await Task.CompletedTask;
        }


        protected virtual async Task DispatchClientMsg(SocketIndentifier s, IMessage msg)
        {
            
        }

        #region 发包

        protected async Task SendPackageIml(SocketIndentifier s, IMessage msg)
        {
            if (msg == null)
                return;
            SunNet.Instance.Log.Info(string.Format("Gateway:SendPackageIml remote:{0} {1} {2}", s.ToString(), msg.GetType(), msg));
            S2SEncodeReq req = new S2SEncodeReq()
            {
                DataObj = msg,
                ProtocolType = EncodeProtocol.Protobuf,
            };
            var encodeAck = await Call<S2SEncodeReq, S2SEncodeAck>(m_encoderService, req);
            SunNet.Instance.SendPackage(s, encodeAck.Buffer);
        }

        protected async Task SendPackageListImpl(SocketIndentifier s, List<IMessage> msgList)
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

        #endregion
    }
}
