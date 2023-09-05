using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SunDonet;
using SunDonet.Protocol;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", 8888);
            ClientLoginReq req = new ClientLoginReq()
            {
                UserName = "abc",
                UserPassword = "123",
            };
            ProtocolDictionaryBase m_protocolDictionary = new TestProtocolDictionary();
            var ack = SunDonet.Encoder.EncodeGoogleProtobuf(req, m_protocolDictionary);
            client.GetStream().Write(ack.m_data, 0, ack.m_data.Length);

            client.Close();
        }
    }
}
