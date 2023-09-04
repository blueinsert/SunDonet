using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    class Program
    {


        static void Main(string[] args)
        {
            SunNet.CreateInstance();
            SunNet.Instance.Start();
            SunNet.Instance.NewService("Main");
            SunNet.Instance.Wait();
        }
    }
}
