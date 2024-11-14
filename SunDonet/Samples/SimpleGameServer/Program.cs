using SunDonet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bluebean.SimpleGameServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SimpleGameServer.CreateInstance();
            if (!SimpleGameServer.Instance.Initialize())
            {
                SimpleGameServer.Instance.Log.Info("SunNet Initialize failed!");
                return;
            }
            SimpleGameServer.Instance.Start();
            SimpleGameServer.Instance.Wait();
        }
    }
}
