using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet.DB
{
    public class DBMethod
    {
        public static async Task<DBCollectionAccount> GetAccount(string userId)
        {
            var filter = Builders<DBCollectionAccount>.Filter.Where(cg => cg._id == userId);

            return await SunNet.Instance.DBHelper.CollectionFindOneAsync<DBCollectionAccount>(DBCollectionName.AccountCollection, filter);
        }

        public static async Task<bool> CreateAccount(string userId,string password)
        {
            DBCollectionAccount account = new DBCollectionAccount()
            {
                _id = userId,
                Password = password,
            };

            return await SunNet.Instance.DBHelper.CollectionInsertOneAsync<DBCollectionAccount>(DBCollectionName.AccountCollection, account);
        }
    }
}
