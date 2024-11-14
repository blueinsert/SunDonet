using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NetLibClient;
using SimpleGameServer.Protocol;
using SunDonet.Protocol;
using Debug = System.Console;

namespace TestClient
{
    public class PlayerContext : IClientEventHandler
    {
        private Client m_client;

        public PlayerContext()
        {
            m_client = new Client(this, new SimpleGameServerProtocolDictionary());
        }

        public void Connect(string ip, int port)
        {
            m_client.Connect(ip, port);
        }

        public void Tick()
        {
            m_client.Tick();
        }

        public void SendLoginReq(string userId,string password)
        {
            LoginReq req = new LoginReq()
            {
                UserName = userId,
                UserPassword = password,
            };
            m_client.SendMessage(req);
        }

        public void SendCreateAccountReq(string userId, string password)
        {
            CreateAccountReq req = new CreateAccountReq()
            {
                UserName = userId,
                UserPassword = password,
            };
            m_client.SendMessage(req);
        }

        public void SendPlayerInfoInitReq()
        {
            PlayerInfoInitReq req = new PlayerInfoInitReq();
            m_client.SendMessage(req);
        }

        public void OnError(int err, string excepionInfo = null)
        {
          Debug.WriteLine(string.Format("err:{0},excepionInfo:{1}", err, excepionInfo));
        }

        public void OnMessage(object msg, int msgId)
        {
            Debug.WriteLine(string.Format("PlayerContext:OnMessage {0} {1}", msg.GetType(), msg));
            switch (msgId)
            {
                case SimpleGameServerProtocolDictionary.MsgId_LoginAck:
                    HandleLoginAck(msg as LoginAck);
                    break;
                case SimpleGameServerProtocolDictionary.MsgId_CreateAccountAck:
                    HandleCreateAccountAck(msg as CreateAccountAck);
                    break;
                case SimpleGameServerProtocolDictionary.MsgId_PlayerInfoInitAck:
                    HandlePlayerInfoInitAck(msg as PlayerInfoInitAck);
                    break;
                case SimpleGameServerProtocolDictionary.MsgId_PlayerBasicInfoNtf:
                    HandlePlayerBasicInfoInitNtf(msg as PlayerBasicInfoNtf);
                    break;
                case SimpleGameServerProtocolDictionary.MsgId_PlayerInfoInitEndNtf:
                    HandlePlayerInfoInitEndNtf(msg as PlayerInfoInitEndNtf);
                    break;
            }


        }

        private void HandleLoginAck(LoginAck msg)
        {
            if (EventOnLoginAck != null)
            {
                EventOnLoginAck(msg.Result);
            }
        }

        private void HandleCreateAccountAck(CreateAccountAck msg)
        {
            if (EventOnCreateAccountAck != null)
            {
                EventOnCreateAccountAck(msg.Result);
            }
        }

        private void HandlePlayerInfoInitAck(PlayerInfoInitAck msg)
        {
            if (EventOnPlayerInfoInitAck != null)
            {
                EventOnPlayerInfoInitAck(msg.Result);
            }
        }

        private void HandlePlayerBasicInfoInitNtf(PlayerBasicInfoNtf msg)
        {
            if (EventOnPlayerBacisInfoNtf != null)
            {
                EventOnPlayerBacisInfoNtf(msg.Result);
            }
        }

        private void HandlePlayerInfoInitEndNtf(PlayerInfoInitEndNtf msg)
        {
            if (EventOnPlayerInitEndNtf != null)
            {
                EventOnPlayerInitEndNtf(msg.Result);
            }
        }

        public event Action<int> EventOnLoginAck;
        public event Action<int> EventOnCreateAccountAck;
        public event Action<int> EventOnPlayerInfoInitAck;
        public event Action<int> EventOnPlayerBacisInfoNtf;
        public event Action<int> EventOnPlayerInitEndNtf;
    }
}
