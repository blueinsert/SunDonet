using MongoDB.Driver;
using SunDonet.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SunDonet.Protocol;
using Google.Protobuf;

namespace SunDonet
{
    public class PlayerContext : IComponentOwner
    {
        private ComponenetManager<IServerComponent> m_components;

        public String m_gameUserId;
        /// <summary>
        /// 计时器
        /// </summary>
        private Stopwatch m_stopWatch = new Stopwatch();

        private MongoPartialDBUpdateDocumentBuilder<DBCollectionPlayer> m_collectionUpdateBuilder;

        public const int DBSavePeroid = 5;//senconds

        private DateTime m_lastDBSaveTime;

        protected PlayerBasicInfoComponent m_basicInfoComponent = null;

        private Agent m_agent;

        public void SetAgent(Agent agent)
        {
            m_agent = agent;
        }

        public PlayerContext() {
            m_components = new ComponenetManager<IServerComponent>(this);
            m_collectionUpdateBuilder = new MongoPartialDBUpdateDocumentBuilder<DBCollectionPlayer>();
            m_lastDBSaveTime = DateTime.Now;
        }

        /// <summary>
        /// 为玩家添加组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T AddOwnerComponent<T>() where T : class, IComponent, new()
        {
            return m_components.Add<T>();
        }

        /// <summary>
        /// 移除组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RemoveOwnerComponent<T>() where T : class, IComponent
        {
            m_components.Remove<T>();
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOwnerComponent<T>() where T : class, IComponent
        {
            return m_components.GetComponent<T>();
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        /// <param name="componentName"></param>
        /// <returns></returns>
        public IComponent GetOwnerComponent(string componentName)
        {
            return m_components.GetComponent(componentName);
        }

        protected virtual void AddComponents()
        {
            m_basicInfoComponent = AddOwnerComponent<PlayerBasicInfoComponent>();

            m_components.PostInitComponents();
        }

        private DBCollectionPlayer CreateNewDBPlayer(string userId)
        {
            DBCollectionPlayer player = new DBCollectionPlayer()
            {
                _id = userId,
                BasicInfo = new DBStructurePlayerBasicInfo() {
                    Name = userId,
                    PlayerLevel = 1,
                    Exp = 0,
                    Energy = 150,
                    Gold = 9999,
                }
            };
            return player;
        }

        protected virtual async Task<DBCollectionPlayer> LoadDBData() {
            var player = await DBMethod.GetPlayer(m_gameUserId);
            if(player == null)
            {
                //新用户
                player = CreateNewDBPlayer(m_gameUserId);
                await DBMethod.CreatePlayer(player);
            }
            return player;
        }

        /// <summary>
        /// 更新db
        /// </summary>
        private void Commit2DB()
        {
            try
            {
                if (m_collectionUpdateBuilder.IsNeedUpdate())
                {
                    var filter = Builders<DBCollectionPlayer>.Filter.Eq(DBConstDefine.PlayerCollectionKeyName, m_gameUserId);
                    SunNet.Instance.DBHelper.CollectionUpdateOne(DBCollectionName.PlayerCollection, filter, m_collectionUpdateBuilder.PartialBuilder);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                m_collectionUpdateBuilder.Clear();
            }
        }

        private void OnCommit2DBEnd() {
            foreach(var comp in m_components.GetAllcomponents())
            {
                var datasectionOwner = comp as IDataSectionOwner;
                if (datasectionOwner != null)
                {
                    datasectionOwner.OnDataSectionSaveEnd();
                }
            }
        }

        public virtual void Save2DB()
        {
            m_stopWatch.Restart();
            //SunNet.Instance.Log.Info("PlayerContext:Save2DB");
            //收集需要更新的字段
            bool save = m_components.SerializeComponents(m_collectionUpdateBuilder);
            if (save)
            {
                //实际进行更新
                Commit2DB();
                OnCommit2DBEnd();
            }
            m_stopWatch.Stop();
            if (m_stopWatch.Elapsed.TotalMilliseconds >= 100)
            {
                Console.Write("PlayerContext::Save2DB userId:{0} cost {1} TotalMilliseconds", this.m_gameUserId, m_stopWatch.Elapsed.TotalMilliseconds);
            }
        }

        protected virtual void InitComponentsFromDB(DBCollectionPlayer dbPlayer)
        {
            m_components.DeSerializeComponents(dbPlayer);
            m_components.PostDeSerializeComponents();
        }

        protected virtual void OnInitComponentsEnd()
        {
            
        }

        public async Task OnLoginOK()
        {
            AddComponents();

            var player =  await LoadDBData();
            if (player != null)
            {
                InitComponentsFromDB(player);
                OnInitComponentsEnd();
            }
            await Task.CompletedTask;
        }

        public async Task OnTick(float deltaTime)
        {
            //SunNet.Instance.Log.Info(string.Format("PlayerContext:OnTick deltaTime:{0}", deltaTime));
            if((DateTime.Now - m_lastDBSaveTime).TotalSeconds > DBSavePeroid)
            {
                Save2DB();
                m_lastDBSaveTime = DateTime.Now;
            }
        }

        private void SendPackage(IMessage msg)
        {
            m_agent.SendPackage(msg);
        }

        private void SendPackageList(List<IMessage> msgList)
        {
            m_agent.SendPackageList(msgList);
        }

        public async Task HandlePlayerInfoInitReq(PlayerInfoInitReq req)
        {
            SunNet.Instance.Log.Info(string.Format("PlayerContext:HandlePlayerInfoInitReq {0}", req));
            PlayerInfoInitAck ack = new PlayerInfoInitAck() { Result = 0};
            SendPackage(ack);

            List<object> messageList = new List<object>();

            m_basicInfoComponent.SyncInitDataToClient(messageList);
            //...

            List<IMessage> msgList = new List<IMessage>();
            foreach(var obj in messageList)
            {
                msgList.Add(obj as IMessage);
            }
            SendPackageList(msgList);

            PlayerInfoInitEndNtf endNtf = new PlayerInfoInitEndNtf() { Result = 0 };
            SendPackage(endNtf);
        }
    }
}
