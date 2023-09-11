using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class ErrorCode
    {
        public const int OK = 0;
        public const int LoginPasswordError = 10;
        public const int LoginAccountNotExist = 11;
        public const int LoginHasBeenLogin = 12;//已被登录
        public const int LoginMultiLogin = 13;//重复登录
    }
}
