using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLibClient
{
    interface IConnectEventHandler
    {
        void OnConnected();

        void OnDisconnected();
    }
}
