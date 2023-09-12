using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    /// <summary>
    /// 管理员控制台
    /// </summary>
    public class AdminConsole : ServiceBase
    {
        public AdminConsole(int id) : base(id) { }

        private SocketIndentifier m_currSocket;

        public override void OnInit()
        {
            base.OnInit();
            SunNet.Instance.Listen(8887, this.m_id);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override Task OnClientConnect(SocketIndentifier s)
        {
            Debug.Log("AdminConsole:OnClientConnect {0}", s.ToString());
            if (m_currSocket == null)
            {
                m_currSocket = s;
            }
            else
            {
                if (m_currSocket != s)
                {
                    Send(s, "ignore, has another been using");
                    //s.Disconnect(false);
                    //s.Close();
                }
            }
            return base.OnClientConnect(s);
        }

        public override Task OnClientDisconnect(SocketIndentifier s, string reason)
        {
            Debug.Log("AdminConsole:OnClientDisconnect {0} {1}", s.ToString(),reason);
            if(m_currSocket == s)
            {
                m_currSocket = null;
            }            
            return base.OnClientDisconnect(s, reason);
        }

        private void Send(SocketIndentifier s, string msg)
        {
            var data = System.Text.UTF8Encoding.Default.GetBytes(msg);
            Send(s, data);
        }

        private void Send(SocketIndentifier s, byte[] data)
        {
            ClientBuffer buffer = ClientBuffer.GetBuffer(data.Length);
            Array.Copy(data, 0, buffer.m_buffer, 0, data.Length);
            SunNet.Instance.Send(s, buffer);
        }

        private void Send(byte[] data)
        {
            Send(m_currSocket, data);
        }

        private void Send(string msg)
        {
            Send(m_currSocket, msg);
        }

        public override Task OnClientData(SocketIndentifier s, ClientBuffer buff)
        {
            Debug.Log("AdminConsole:OnClientData {0}", s.ToString());
            if (m_currSocket == s)
            {
                string command = System.Text.UTF8Encoding.Default.GetString(buff.m_buffer, 0, buff.m_dataLen);
                ExeCommand(command);
            }
            else
            {
                Send(s, "ignore, has another been using");
            }
            return Task.CompletedTask;
        }

        public void ExeCommand(string command)
        {
            Debug.Log("AdminConsole:ExeCommand {0}", command);
            //Send("usage:...");
            if(command == "exit")
            {
                ExeExitCmd();
            }
        }

        private void ExeExitCmd()
        {
            Debug.Log("AdminConsole:ExeExitCmd");
            //释放执行本服务的worker，避免stop等待woker结束，而无法结束
            Task.Run(() => { SunNet.Instance.Stop(); });
        }
    }
}
