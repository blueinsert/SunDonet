using Google.Protobuf;
using SunDonet.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class GoogleProtobufHelper
    {
        /// <summary>
        /// 协议头部长度
        /// </summary>
        public const int ProtocolHeaderLen = sizeof(uint) * 2;

        /// <summary>
        /// 对象序列化，目标是一个stream
        /// </summary>
        /// <param name="pkgObj"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static void SerializeObject(object pkgObj, Stream stream)
        {
            // 获取Descriptor属性
            try
            {
                if (pkgObj is IMessage)
                {
                    ((IMessage)pkgObj).WriteTo(stream);
                }
            }
            // 这个错误，外面会有捕捉，就不打印日志了，
            catch (NotSupportedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SunNet.Instance.Log.Info("ex={0}", ex);
                throw;
            }
        }

        /// <summary>
        /// 反序列化包
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static object DeserializeMsgByType(Type dataType, Stream stream)
        {
            try
            {
                var ccMsg = (IMessage)Activator.CreateInstance(dataType);
                ccMsg.MergeFrom(stream);
                return ccMsg;

            }
            catch (Exception ex)
            {
                SunNet.Instance.Log.Info("ex={0}", ex);
                throw;
            }
        }

        public static ClientBuffer EncodeMessage(IMessage imessage, IProtoProvider protocolProvider)
        {

            ClientBuffer buff = ClientBuffer.GetBuffer(8 * 1024 * 5);
            int headLen = ProtocolHeaderLen;
            uint pkgLen;
            int dataOffset = 0;
            {
                dataOffset += headLen;
                using (var stream = new MemoryStream(buff.m_buffer, dataOffset, buff.m_buffer.Length - dataOffset))
                {
                    GoogleProtobufHelper.SerializeObject(imessage, stream);
                    pkgLen = (uint)(stream.Position + headLen);
                    dataOffset += (int)stream.Position;
                }
                using (var headerStream = new MemoryStream(buff.m_buffer, 0, headLen))
                {
                    using (var writer = new BinaryWriter(headerStream))
                    {
                        //获取协议id
                        uint protocolId = (uint)protocolProvider.GetIdByType(imessage.GetType());
                        writer.Write(pkgLen);
                        writer.Write(protocolId);
                    }
                }
            }
            buff.m_dataLen = dataOffset;
            return buff;
        }

        public static int DecodeMessage(byte[] data, int dataLen, IProtoProvider protocolProvider, out object msgObj)
        {
            msgObj = null;
            int byteHandled = 0;
            if (data == null || dataLen < 1)
            {
                return 0;
            }
            int dataOffset = 0;
            if (dataLen - dataOffset < ProtocolHeaderLen)
            {
                return 0;
            }
            Type msgType = null;
            Object deserializeObject = null;
            MemoryStream deserializeBuff = null;

            var msgFullLength = BitConverter.ToUInt32(data, dataOffset);
            if (dataLen < msgFullLength)
            {
                return 0;
            }
            const int uintLength = sizeof(uint);
            dataOffset += uintLength;
            var msgId = BitConverter.ToUInt32(data, dataOffset);
            dataOffset += uintLength;
            try
            {
                //获取消息类型
                msgType = protocolProvider.GetTypeById((int)msgId);
                int protoLength = (int)(msgFullLength - ProtocolHeaderLen);
                deserializeBuff = new MemoryStream(data, dataOffset, protoLength);
                deserializeObject = GoogleProtobufHelper.DeserializeMsgByType(msgType, deserializeBuff);

                byteHandled = (int)msgFullLength;
                msgObj = deserializeObject;
            }
            catch (Exception e)
            {
                SunNet.Instance.Log.Info(string.Format("Wrong Msg Id:{0} {1}", msgId, e.ToString()));
            }
            return byteHandled;
        }
    }
}
