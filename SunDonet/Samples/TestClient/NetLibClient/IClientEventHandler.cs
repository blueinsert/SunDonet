using System;
using System.Collections.Generic;
using System.Text;

namespace NetLibClient
{
    /// <summary>
    /// 本地消息定义
    /// </summary>
    public enum ClientErrId
    {

        /// <summary>
        /// 连接断开
        /// </summary>
        ConnectionBreak = 9002,
        /// <summary>
        /// 连接失败
        /// </summary>
        ConnectionFailure = 9003,
        /// <summary>
        /// 连接发送失败
        /// </summary>
        ConnectionSendFailure = 9004,
        /// <summary>
        /// 连接接收失败
        /// </summary>
        ConnectionRecvFailure = 9005,
    }

    public interface IClientEventHandler
    {

        //void OnConnected();

        //void OnDisconnected();

        void OnError(Int32 err, String excepionInfo = null);

        void OnMessage(Object msg, Int32 msgId);
    }
}
