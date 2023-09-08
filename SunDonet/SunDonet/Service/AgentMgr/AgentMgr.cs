using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    /// <summary>
    /// 查询，注册 agent
    /// </summary>
    public class AgentMgr:ServiceBase
    {
        //socket - agent id
        private Dictionary<Socket, int> m_socket2PlayerDic = new Dictionary<Socket, int>();
        private Dictionary<int, Socket> m_player2SocketDic = new Dictionary<int, Socket>();

        public AgentMgr(int id) : base(id) { }
    }
}
