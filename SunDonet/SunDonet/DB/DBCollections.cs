using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet.DB
{
    public class DBCollectionAccount
    {
        public string _id { get; set; }

        [BsonElement("Password")]
        public string Password { get; set; }
    }

    public class DBCollectionPlayer
    {
        public string _id { get; set; }

        [BsonElement("Basic")]
        public DBStructurePlayerBasicInfo BasicInfo = new DBStructurePlayerBasicInfo();
    }


}
