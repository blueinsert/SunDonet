using Google.Protobuf;
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
                Console.WriteLine("ex={0}", ex);
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
                Console.WriteLine("ex={0}", ex);
                throw;
            }
        }
    }
}
