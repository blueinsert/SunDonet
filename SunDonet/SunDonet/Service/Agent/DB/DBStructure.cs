using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet.DB
{
    public class DBStructurePlayerBasicInfo
    {
        [BsonElement("Name")]
        public string Name {
            get;
            set;
        }

        [BsonElement("Level")]
        public Int32 PlayerLevel
        {
            get;
            set;
        }

        [BsonElement("Exp")]
        public Int32 Exp
        {
            get;
            set;
        }

        /// <summary>
        /// 体力
        /// </summary>
        [BsonElement("Energy")]
        public Int32 Energy
        {
            get;
            set;
        }

        [BsonElement("Gold")]
        public Int32 Gold
        {
            get;
            set;
        }

        /// <summary>
        /// db版本号
        /// </summary>
        [BsonElement("Version")]
        public UInt16 Version
        {
            get;
            set;
        }

        /// <summary>
        /// 冗余或者过时的数据，避免报错
        /// </summary>
        [BsonExtraElements()]
        public BsonDocument _Redundancies { get; set; }
    }
}
