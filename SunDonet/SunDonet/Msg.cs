using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;

namespace SunDonet
{
    public abstract class MsgBase
    {
        public enum MsgType
        {
            Service,//service 2 service
            Socket_Accept,
            Socket_Disconnect,
            Socket_Data,
        }

        public MsgType m_type;
    }

    public class ServiceMsg : MsgBase
    {
        public int m_source;
        public byte[] m_data;
    }

    public class SocketAcceptMsg : MsgBase
    {
        public Socket m_listen;
        public Socket m_client;
    }

    public class SocketDisconnectMsg : MsgBase
    {
        public Socket m_client;
    }

    public class SocketDataMsg : MsgBase
    {
        public Socket m_socket;
        public byte[] m_data;
    }
}
