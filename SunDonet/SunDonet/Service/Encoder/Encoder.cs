using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using SunDonet.Protocol;

namespace SunDonet
{
    public class Encoder : ServiceBase
    {
        public Encoder(int id) : base(id) { }

        private ProtocolDictionaryBase m_protocolDictionary;

        public override void OnInit()
        {
            base.OnInit();
            m_protocolDictionary = SunNet.Instance.ProtocolDic;

            RegisterServiceMsgCallHandler<S2SEncodeReq, S2SEncodeAck>(HandleEncodeReq);
            RegisterServiceMsgCallHandler<S2SDecodeReq, S2SDecodeAck>(HandleDecodeReq);
        }

        public static S2SEncodeAck EncodeGoogleProtobuf(IMessage imessage, ProtocolDictionaryBase protocolDictionary)
        {
            var buff = GoogleProtobufHelper.EncodeMessage(imessage, protocolDictionary);
            S2SEncodeAck ack = new S2SEncodeAck()
            {
                Buffer = buff,
            };
            return ack;
        }

        private async Task<S2SEncodeAck> HandleEncodeReq(S2SEncodeReq req)
        {
            //Console.WriteLine(string.Format("Encoder:HandleEncodeReq"));
            if (req.ProtocolType == EncodeProtocol.Protobuf)
            {
                return EncodeGoogleProtobuf(req.DataObj as IMessage, m_protocolDictionary);
            }
            return null;
        }

        public static S2SDecodeAck DecodeGoogleProtobuf(byte[] data, int dataLen, ProtocolDictionaryBase protocolDictionary)
        {
            object msgObj = null;
            int byteHandled = GoogleProtobufHelper.DecodeMessage(data, dataLen, protocolDictionary, out msgObj);

            S2SDecodeAck ack = new S2SDecodeAck()
            {
                ByteLenHandled = byteHandled,
                DataObj = msgObj,
            };
            return ack;
        }

        private async Task<S2SDecodeAck> HandleDecodeReq(S2SDecodeReq req)
        {
            //Console.WriteLine(string.Format("Encoder:HandleDecodeReq"));
            if (req.ProtocolType == EncodeProtocol.Protobuf)
            {
                return DecodeGoogleProtobuf(req.Buffer.m_buffer, req.Buffer.m_dataLen, m_protocolDictionary);
            }
            return null;
        }
    }
}
