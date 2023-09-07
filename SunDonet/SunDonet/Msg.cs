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
        public ClientBuffer m_buff;
    }

    public abstract class ServiceMsg : MsgBase
    {
        public int m_source;
    }

    //不需要回复
    public abstract class ServiceMsgNtf : ServiceMsg
    {
    }

    public abstract class ServiceMsgReq : ServiceMsg
    {
        public int m_token;
    }

    public abstract class ServiceMsgAck : ServiceMsg
    {
        public int m_token;
    }

    public  class NullServiceMsgAck: ServiceMsgAck
    {

    }

    public abstract class RemoteServiceMsg : ServiceMsg
    {
        byte[] m_data;
    }

    public abstract class RemoteAwaitableServiceMsg : ServiceMsg
    {
        public int m_token;
        byte[] m_data;
    }

}
