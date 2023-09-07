using MongoDB.Driver;
using SunDonet.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class PlayerContext : IComponentOwner
    {
        private ComponenetManager<IComponent> m_components;

        public String m_gameUserId;
        /// <summary>
        /// 计时器
        /// </summary>
        private Stopwatch m_stopWatch = new Stopwatch();

        private MongoPartialDBUpdateDocumentBuilder<DBCollectionPlayer> m_collectionUpdateBuilder;

        public PlayerContext() {
            m_components = new ComponenetManager<IComponent>(this);
            m_collectionUpdateBuilder = new MongoPartialDBUpdateDocumentBuilder<DBCollectionPlayer>();
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
            AddOwnerComponent<PlayerBasicInfoComponent>();
        }

        protected virtual void LoadDBData() { }

        /// <summary>
        /// 更新db
        /// </summary>
        private void Sync2DB()
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

        private void OnSync2DBEnd() { }

        protected virtual void Save2DB()
        {
            m_stopWatch.Restart();
            //收集需要更新的字段
            bool save = m_components.SerializeComponents(m_collectionUpdateBuilder);
            if (save)
            {
                //实际进行更新
                Sync2DB();
                OnSync2DBEnd();
            }
            m_stopWatch.Stop();
            if (m_stopWatch.Elapsed.TotalMilliseconds >= 100)
            {
                Console.Write("PlayerContext::Save2DB userId:{0} cost {1} TotalMilliseconds", this.m_gameUserId, m_stopWatch.Elapsed.TotalMilliseconds);
            }
        }

        protected virtual void InitComponentsFromDB()
        {
            m_components.DeSerializeComponents(m_collectionUpdateBuilder);
            m_components.PostDeSerializeComponents();
        }

        protected virtual void OnInitComponentsEnd()
        {
        }

        public async Task OnLoginOK()
        {
            AddComponents();

            LoadDBData();
            InitComponentsFromDB();
            OnInitComponentsEnd();
            await Task.CompletedTask;
        }
    }
}
