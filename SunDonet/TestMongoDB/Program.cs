using SunDonet.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMongoDB
{
    class Program
    {
        static void Main(string[] args)
        {
            MongoDBConfigInfo dbcfg = new MongoDBConfigInfo()
            {
                DataBase = "MyDB",
                ConnectHost = "127.0.0.1",
                Port = "27017",
                //UserName = "bluebean",
                //Password = "1234",
            };
            try
            {
                var dbHelper = new MongoDBHelper(dbcfg);
                SunNet.Instance.Log.Info("MongoDB connect success");

                DBCollectionPlayer dBCollectionPlayer1 = new DBCollectionPlayer()
                {
                    _id = "zzx001",
                    BasicInfo = new DBStructurePlayerBasicInfo()
                    {
                        Name = "ZZX001",
                        PlayerLevel = 33,
                        Energy = 999,
                    },
                };
                DBCollectionPlayer dBCollectionPlayer2 = new DBCollectionPlayer()
                {
                    _id = "zzx002",
                    BasicInfo = new DBStructurePlayerBasicInfo()
                    {
                        Name = "ZZX002",
                        PlayerLevel = 33,
                        Energy = 999,
                    },
                };
                DBCollectionPlayer dBCollectionPlayer3 = new DBCollectionPlayer()
                {
                    _id = "zzx003",
                    BasicInfo = new DBStructurePlayerBasicInfo()
                    {
                        Name = "ZZX003",
                        PlayerLevel = 33,
                        Energy = 999,
                    },
                };
                dbHelper.CollectionInsertOneIgnoreDuplicateKeyAsync(DBCollectionName.PlayerCollection, dBCollectionPlayer1).Wait();
                dbHelper.CollectionInsertOneIgnoreDuplicateKeyAsync(DBCollectionName.PlayerCollection, dBCollectionPlayer2).Wait();
                dbHelper.CollectionInsertOneIgnoreDuplicateKeyAsync(DBCollectionName.PlayerCollection, dBCollectionPlayer3).Wait();
            }
            catch (Exception e)
            {
                SunNet.Instance.Log.Info("MongoDB connect failed");
                throw e;
            }
        }
    }
}
