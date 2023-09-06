using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet.DB
{
    /// <summary>
    /// 数据库相关定义
    /// </summary>
    public class DBConstDefine
    {
        /// <summary>
        /// 数据库玩家表key的名字
        /// </summary>
        public const string PlayerCollectionKeyName = "_id";
    }

    public class DBCollectionName
    {
        /// <summary>
        /// 账号表表名
        /// </summary>
        public const string AccountCollection = "Account";

        /// <summary>
        /// 玩家数据1表
        /// </summary>
        public const string PlayerCollection = "Player";
    }
}
