﻿using System;
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

        /// <summary>
        /// 协议头部长度
        /// </summary>
        public const int ProtocolHeaderLen = sizeof(uint) * 2;
        private ProtocolDictionaryBase m_protocolDictionary;

        public override void OnInit()
        {
            base.OnInit();
            m_protocolDictionary = new SunDonetProtocolDictionary();
        }

        public override async Task<ServiceMsgAck> OnServiceCall(ServiceMsgReq req)
        {
            if (req is EncodeReq)
            {
                var ack =  await HandleEncodeReq(req as EncodeReq);
                return ack;
            }else if(req is DecodeReq)
            {
                var ack = await HandleDecodeReq(req as DecodeReq);
                return ack;
            }
            return null;
        }

        public static EncodeAck EncodeGoogleProtobuf(IMessage imessage, ProtocolDictionaryBase protocolDictionary)
        {

            byte[] buff = new byte[8 * 1024 * 5];
            int headLen = sizeof(uint) * 2;
            uint pkgLen;
            int dataOffset = 0;
            {
                dataOffset += headLen;
                using (var stream = new MemoryStream(buff, dataOffset, buff.Length - dataOffset))
                {
                    GoogleProtobufHelper.SerializeObject(imessage, stream);
                    pkgLen = (uint)(stream.Position + headLen);
                    dataOffset += (int)stream.Position;
                }
                using (var headerStream = new MemoryStream(buff, 0, headLen))
                {
                    using (var writer = new BinaryWriter(headerStream))
                    {
                        //获取协议id
                        uint protocolId = (uint)protocolDictionary.GetIdByType(imessage.GetType());
                        writer.Write(pkgLen);
                        writer.Write(protocolId);
                    }
                }
            }
            EncodeAck ack = new EncodeAck()
            {
                m_data = buff,
                m_dataLen = dataOffset,
            };
            return ack;
        }

        private async Task<EncodeAck> HandleEncodeReq(EncodeReq req)
        {
            if (req.m_protocolType == EncodeProtocol.Protobuf)
            {
                return EncodeGoogleProtobuf(req.m_dataObj as IMessage, m_protocolDictionary);
            }
            return null;
        }

        private DecodeAck DecodeGoogleProtobuf(byte[] data,int dataLen)
        {
            DecodeAck ack = new DecodeAck()
            {
                m_byteHandled = 0,
                m_dataObj = null,
            };
            if(data==null || dataLen < 1)
            {
                return ack;
            }
            int dataOffset = 0;
            if(dataLen - dataOffset < ProtocolHeaderLen)
            {
                return ack;
            }
            Type msgType = null;
            Object deserializeObject = null;
            MemoryStream deserializeBuff = null;

            var msgFullLength = BitConverter.ToUInt32(data, dataOffset);
            if(dataLen < msgFullLength)
            {
                ack.m_byteHandled = 0;
                return ack;
            }
            const int uintLength = sizeof(uint);
            dataOffset += uintLength;
            var msgId = BitConverter.ToUInt32(data, dataOffset);
            dataOffset += uintLength;
            try{
                //获取消息类型
                msgType = m_protocolDictionary.GetTypeById((int)msgId);
                int protoLength = (int)( msgFullLength - ProtocolHeaderLen);
                deserializeBuff = new MemoryStream(data, dataOffset, protoLength);
                deserializeObject = GoogleProtobufHelper.DeserializeMsgByType(msgType, deserializeBuff);

                ack.m_byteHandled = (int)msgFullLength;
                ack.m_dataObj = deserializeObject;
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Wrong Msg Id:{0} {1}", msgId,e.ToString()));
            }
            return ack;
        }

        private async Task<DecodeAck> HandleDecodeReq(DecodeReq req)
        {
            if (req.m_protocolType == EncodeProtocol.Protobuf)
            {
                return DecodeGoogleProtobuf(req.m_buff.m_buffer, req.m_buff.m_dataLen);
            }
            return null;
        }
    }
}
