using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SunDonet.DB
{
    /// <summary>
    /// 服务器对应的数据库配置节点
    /// </summary>
    [Serializable]
    public class MongoDBConfigInfo
    {
        /// <summary>
        /// 数据库id
        /// </summary>
        [XmlAttribute("Id")]
        public Int32 Id { get; set; }

        /// <summary>
        /// 是否全局数据库
        /// </summary>
        [XmlAttribute("IsGlobal")]
        public bool IsGlobal { get; set; }

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
        /// 是否连接的是mongos
        /// </summary>
        [XmlAttribute("IsMongos")]
        public bool IsMongos { get; set; }

        /// <summary>
        /// 检查两个数据库配置项是否一致
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool CheckSameDBConfig(MongoDBConfigInfo info)
        {
            if (Id != info.Id ||
                DataBase != info.DataBase ||
                ConnectHost != info.ConnectHost ||
                Port != info.Port ||
                ReplicaSet != info.ReplicaSet ||
                UserName != info.UserName ||
                Password != info.Password ||
                IsMongos != info.IsMongos)
            {
                return false;
            }
            return true;
        }
    }

    public class MongoDBHelper
    {
        /// <summary>
        /// 配置文件读到的数据库配置
        /// </summary>
        public MongoDBConfigInfo DBConfig
        {
            get { return m_dbConfig; }
        }

        /// <summary>
        /// 配置文件读到的数据库配置
        /// </summary>
        private MongoDBConfigInfo m_dbConfig;

        /// <summary>
        /// 由配置文件配置数据生成的数据库entry，保存在这里，后面再使用的时候直接从这里取，提升效率
        /// </summary>
        private IMongoDatabase m_gameDBEntry;
        private IMongoDatabase m_adminDBEntry;

        /// <summary>
        /// 是否连接到mongos，决定是否需要使用分片逻辑
        /// </summary>
        private bool m_isConnectMongos;


        public MongoDBHelper(MongoDBConfigInfo dbConfig, ReadPreference readPreference = null)
        {
            m_dbConfig = dbConfig;
            m_gameDBEntry = GetDataBaseEntry(readPreference);
        }

        private IMongoDatabase GetDataBaseEntry(ReadPreference readPreference = null)
        {
            // 如果已经有该dbName对应的MongoDatabase的记录，那么就直接返回
            if (m_gameDBEntry != null)
            {
                return m_gameDBEntry;
            }

            // 判断配置合法性
            if (m_dbConfig == null)
            {
                return null;
            }

            //先从配置中取得配置值
            String replicaSetName = m_dbConfig.ReplicaSet;
            String host = m_dbConfig.ConnectHost;  //"host1:host2:host3"
            String port = m_dbConfig.Port;       //"port1:port2:port3"
            String user = m_dbConfig.UserName;
            String pwd = m_dbConfig.Password;
            String db = m_dbConfig.DataBase;
            String dbName = m_dbConfig.DataBase;
            // 保存配置
            m_isConnectMongos = m_dbConfig.IsMongos;

            //生成mongoDB的连接配置
            var settings = new MongoClientSettings();
            List<MongoServerAddress> servers = new List<MongoServerAddress>();

            // 解析对应的host和端口
            var hostList = host.Split(':');
            var portList = port.Split(':');
            if (hostList.Length != portList.Length)
            {
                throw new ArgumentException("host and port count is not match.");
            }
            // 添加所有的ip和port
            for (var index = 0; index < hostList.Length; index++)
            {
                servers.Add(new MongoServerAddress(hostList[index], Convert.ToUInt16(portList[index])));
            }
            // 赋值server信息
            settings.Servers = servers;

            if (!String.IsNullOrEmpty(replicaSetName))
            {
                settings.ReplicaSetName = replicaSetName;
                settings.ReadPreference = ReadPreference.PrimaryPreferred;
            }
            //settings.ClusterConfigurator = builder =>
            //{
            //    builder.ConfigureCluster(a => a.With(serverSelectionTimeout: TimeSpan.FromSeconds(10)));
            //};
            settings.ConnectionMode = ConnectionMode.Automatic;
            settings.MaxConnectionPoolSize = 50;
            settings.WaitQueueSize = 20000;
            settings.ConnectTimeout = TimeSpan.FromSeconds(2);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            if (readPreference != null)
            {
                settings.ReadPreference = readPreference;
            }
            if (!String.IsNullOrEmpty(user) && !String.IsNullOrEmpty(pwd))
            {
                settings.Credentials = new MongoCredential[]{
                MongoCredential.CreateCredential("admin", user, pwd)
                };
            }

            settings.Freeze();


            //获得数据库entry
            m_gameDBEntry = new MongoClient(settings).GetDatabase(dbName);
            m_adminDBEntry = new MongoClient(settings).GetDatabase("admin");
            return m_gameDBEntry;
        }

        public IMongoCollection<T> GetCollection<T>(String collectionName)
        {
            try
            {
                return GetDataBaseEntry().GetCollection<T>(collectionName);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        public void CreateCollection(String collectionName)
        {
            try
            {
                GetDataBaseEntry().CreateCollection(collectionName);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        public void DropCollection(String collectionName)
        {
            try
            {
                GetDataBaseEntry().DropCollection(collectionName);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 插入一个记录
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="document"></param>
        /// <returns></returns>
        public bool CollectionInsertOne<TDocument>(String collectionName, TDocument document)
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    coll.InsertOne(document);
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 插入一个记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<bool> CollectionInsertOneAsync<TDocument>(String collectionName, TDocument document, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    await coll.InsertOneAsync(document, null, cancellationToken).ConfigureAwait(false);
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 插入一个记录，无视DuplicateKey
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<bool> CollectionInsertOneIgnoreDuplicateKeyAsync<TDocument>(String collectionName, TDocument document, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    await coll.InsertOneAsync(document, null, cancellationToken).ConfigureAwait(false);
                    return true;
                }
                return false;
            }
            catch (MongoWriteException we)
            {
                if (we.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    return true;
                }
                else
                {
                    throw we;
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 删除符合filter的document个数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public Int64 CollectionDeleteMany<TDocument>(String collectionName, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var delResult = coll.DeleteMany(filter, cancellationToken);
                    return delResult.DeletedCount;
                }
                return 0;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 异步删除符合filter的document个数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<Int64> CollectionDeleteManyAsync<TDocument>(String collectionName, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var delResult = await coll.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);
                    return delResult.DeletedCount;
                }
                return 0;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 删除符合filter的document个数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<Int64> CollectionDeleteOneAsync<TDocument>(String collectionName, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var delResult = await coll.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
                    return delResult.DeletedCount;
                }
                return 0;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 同步的更新方法
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Int64 CollectionUpdateOne<TDocument>(String collectionName, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var updateResult = coll.UpdateOne(filter, update, null, cancellationToken);
                    return updateResult.ModifiedCount;
                }
                return 0;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 更新符合filter的document个数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<Int64> CollectionUpdateOneAsync<TDocument>(String collectionName, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var delResult = await coll.UpdateOneAsync(filter, update, null, cancellationToken).ConfigureAwait(false);
                    return delResult.ModifiedCount;
                }
                return 0;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// find and update, if not find, insert one
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public TDocument CollectionFindOneAndUpdate<TDocument>(String collectionName, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    MongoDB.Driver.FindOneAndUpdateOptions<TDocument> option = new MongoDB.Driver.FindOneAndUpdateOptions<TDocument>();
                    option.IsUpsert = true;

                    var updateResult = coll.FindOneAndUpdate(filter, update, option, cancellationToken);
                    return updateResult;
                }
                return default(TDocument);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 返回符合filter的首个document
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<TDocument> CollectionFindOneAsync<TDocument>(String collectionName, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var document = await coll.Find<TDocument>(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    return document;
                }
                return default(TDocument);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 同步的FindOne
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public TDocument CollectionFindOne<TDocument>(String collectionName, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var document = coll.Find<TDocument>(filter).FirstOrDefault(cancellationToken);
                    return document;
                }
                return default(TDocument);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 同步的FindOne，使用project
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public TProjection CollectionFindOne<TDocument, TProjection>(String collectionName, FilterDefinition<TDocument> filter,
            ProjectionDefinition<TDocument, TProjection> projection = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    FindOptions<TDocument, TProjection> options = new FindOptions<TDocument, TProjection>();
                    options.Projection = projection;
                    var document = coll.FindSync<TProjection>(filter, options).FirstOrDefault(cancellationToken);
                    return document;
                }
                return default(TProjection);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 异步的FindMany
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<TDocument>> CollectionFindManyAsync<TDocument>(String collectionName, FilterDefinition<TDocument> filter, int? limit = null, int? skip = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var document = await coll.Find<TDocument>(filter).Limit(limit).Skip(skip).ToListAsync().ConfigureAwait(false);
                    return document;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 同步的FindMany
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="limit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public List<TDocument> CollectionFindMany<TDocument>(String collectionName, FilterDefinition<TDocument> filter, int? limit = null, int? skip = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    var document = coll.Find<TDocument>(filter).Limit(limit).Skip(skip);
                    return document.ToList<TDocument>();
                }
                return null;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 同步的FindMany，使用projection
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="limit"></param>
        /// <param name="skip"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IAsyncCursor<TProjection> CollectionFindManyCursor<TDocument, TProjection>(String collectionName,
            FilterDefinition<TDocument> filter,
            ProjectionDefinition<TDocument, TProjection> projection = null,
            SortDefinition<TDocument> sort = null,
            int? limit = null, int? skip = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    FindOptions<TDocument, TProjection> options = new FindOptions<TDocument, TProjection>();
                    options.Limit = limit;
                    options.Projection = projection;
                    options.Skip = skip;
                    options.Sort = sort;
                    var document = coll.FindSync<TProjection>(filter, options, cancellationToken);
                    return document;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 同步的Count
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public long CollectionCount<TDocument>(String collectionName, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    return coll.Find<TDocument>(filter).Count();
                }
                return 0;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 异步的Count
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<long> CollectionCountAsync<TDocument>(String collectionName, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var coll = GetCollection<TDocument>(collectionName);
                if (coll != null)
                {
                    return await coll.Find<TDocument>(filter).CountAsync().ConfigureAwait(false);
                }
                return 0;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }
    }

    public interface IDBUpdateBuilder
    {
        /// <summary>
        /// 是否需要更新
        /// </summary>
        /// <returns></returns>
        bool IsNeedUpdate();

        /// <summary>
        /// 清理
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 部分更新docment中的字段
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class MongoPartialDBUpdateDocumentBuilder<TDocument> : IDBUpdateBuilder
    {
        /// <summary>
        /// 是否需要更新
        /// </summary>
        /// <returns></returns>
        public bool IsNeedUpdate()
        {
            return PartialBuilder != null;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            PartialBuilder = null;
        }

        /// <summary>
        /// documents的filter field
        /// </summary>
        public string FilterField { get; set; }

        /// <summary>
        /// key为filed名
        /// </summary>
        public UpdateDefinition<TDocument> PartialBuilder { get; set; }

        /// <summary>
        /// 设置field
        /// </summary>
        /// <typeparam name="TField"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public MongoPartialDBUpdateDocumentBuilder<TDocument> Set<TField>(Expression<Func<TDocument, TField>> field, TField value)
        {
            if (PartialBuilder == null)
            {
                PartialBuilder = Builders<TDocument>.Update.Set(field, value);
            }
            else
            {
                PartialBuilder = PartialBuilder.Set(field, value);
            }

            return this;
        }
    }


}
