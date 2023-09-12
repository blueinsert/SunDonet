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

        public MsgType MessageType;
    }

    public class SocketAcceptMsg : MsgBase
    {
        public SocketIndentifier Listen;
        public SocketIndentifier Client;
    }

    public class SocketDisconnectMsg : MsgBase
    {
        public SocketIndentifier ClientId;
        public string Reason;
    }

    public class SocketDataMsg : MsgBase
    {
        public SocketIndentifier SocketId;
        public ClientBuffer Buff;
    }

    public abstract class ServiceMsg : MsgBase
    {
        public int Source;
    }

    //不需要回复
    public abstract class ServiceMsgNtf : ServiceMsg
    {
    }

    public class ServiceTickMsgNtf: ServiceMsgNtf
    {
        private ServiceTickMsgNtf() { }
        public static readonly ServiceTickMsgNtf TickMsgNtf = new ServiceTickMsgNtf();
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
