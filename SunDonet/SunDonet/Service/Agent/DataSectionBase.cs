using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class DataSectionBase
    {
        public UInt16 Version
        {
            get
            {
                return m_version;
            }
        }

        /// <summary>
        /// 当前内存版本
        /// </summary>
        protected UInt16 m_version = 1;
        /// <summary>
        /// 数据库已提交版本
        /// </summary>
        protected UInt16 m_dbCommitedVersion = 1;
        /// <summary>
        /// 客户端版本
        /// </summary>
        protected UInt16 m_clientVersion = 1;
        /// <summary>
        /// 客户端已提交版本
        /// </summary>
        protected UInt16 m_clientCommitedVersion = 1;

        public void InitVersion(UInt16 dbVersion)
        {
            m_version = dbVersion == 0 ? (UInt16)1 : dbVersion;
            m_dbCommitedVersion = m_clientVersion = m_clientCommitedVersion = m_version;
        }

        public void SetDirty(bool needCommit2Client = false)
        {
            m_version++;
            if (needCommit2Client)
            {
                m_clientVersion++;
            }
        }

        public bool NeedSync2DB()
        {
            return m_dbCommitedVersion != m_version;
        }

        public void OnDBSynced()
        {
            m_dbCommitedVersion = m_version;
        }


        public void SetClientCommitedVersion(UInt16 version)
        {
            m_clientCommitedVersion = version;
        }

        public bool NeedSyncToClient()
        {
            return m_clientCommitedVersion != m_clientVersion;
        }

        public void OnClientSynced()
        {
            m_clientCommitedVersion = m_clientVersion;
        }

        public virtual object SerializeToClient()
        {
            return null;
        }
    }

    public interface IDataSectionOwner
    {
        void OnDataSectionSaveEnd();

        /// <summary>
        /// PlayerInfoInitReq中发送最新的ds给客户端
        /// </summary>
        /// <param name="syncDestDatas"></param>
        void SyncInitDataToClient(List<object> syncDestDatas);
    }
}
