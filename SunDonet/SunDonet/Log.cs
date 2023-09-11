using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public static class Debug
    {
        public static void Log(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
                SunNet.Instance.Log.Info(format);
            else
                SunNet.Instance.Log.InfoFormat(format, args);
        }

        public static void Error(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
                SunNet.Instance.Log.Error(format);
            else
                SunNet.Instance.Log.ErrorFormat(format, args);
        }
    }
}
