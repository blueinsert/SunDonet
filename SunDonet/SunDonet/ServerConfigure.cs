using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SunDonet
{
    /// <summary>
    /// 配置文件根节点。
    /// </summary>
    [Serializable]
    [XmlRoot("Configure")]
    public class ServerConfig
    {
        [XmlElement("DataConfig")]
        public DataConfig DataConfig { get; set; }

        /// <summary>
        /// 日志配置信息。
        /// </summary>
        [XmlElement("Log")]
        public Log Log { get; set; }

        /// <summary>
        /// 日志配置信息。
        /// </summary>
        [XmlElement("MongoDB")]
        public MongoDBConfigInfo DBConfig { get; set; }
    }

    /// <summary>
    /// 日志配置节点。
    /// </summary>
    [Serializable]
    public class Log
    {
        /// <summary>
        /// 是否开启文件日志。
        /// </summary>
        [XmlAttribute("LogConfigPath")]
        public string LogConfigPath { get; set; }
    }

    /// <summary>
    /// 服务器对应的数据库配置节点
    /// </summary>
    [Serializable]
    public class MongoDBConfigInfo
    {
        /// <summary>
        /// 数据库id
        /// </summary>
        //[XmlAttribute("Id")]
        //public Int32 Id { get; set; }

        /// <summary>
        /// 是否全局数据库
        /// </summary>
        //[XmlAttribute("IsGlobal")]
        //public bool IsGlobal { get; set; }

        /// <summary>
        /// 数据库名
        /// </summary>
        [XmlAttribute("DataBase")]
        public String DataBase { get; set; }

        /// <summary>
        /// 目标的ip信息
        /// </summary>
        [XmlAttribute("ConnectHost")]
        public String ConnectHost { get; set; }

        /// <summary>
        /// 目标端口信息
        /// </summary>
        [XmlAttribute("Port")]
        public String Port { get; set; }

        /// <summary>
        /// 使用的ReplicaSet name，生产环境必须使用
        /// </summary>
        [XmlAttribute("ReplicaSet")]
        public String ReplicaSet { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [XmlAttribute("UserName")]
        public String UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [XmlAttribute("Password")]
        public String Password { get; set; }

        /// <summary>
        /// 检查两个数据库配置项是否一致
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool CheckSameDBConfig(MongoDBConfigInfo info)
        {
            if (
                //Id != info.Id ||
                DataBase != info.DataBase ||
                ConnectHost != info.ConnectHost ||
                Port != info.Port ||
                ReplicaSet != info.ReplicaSet ||
                UserName != info.UserName ||
                Password != info.Password)
            {
                return false;
            }
            return true;
        }
    }

    [Serializable]
    public class DataConfig
    {
        /// <summary>
        /// 游戏配置数据路径
        /// </summary>
        [XmlAttribute("DataPath")]
        public String ServerDataPath { get; set; }
    }
}
