using MongoDB.Driver;
using SunDonet.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class PlayerBasicInfoComponentCommon : IComponent, IDataSectionOwner
    {
        private IComponentOwner m_owner;
        public IComponentOwner Owner { get => m_owner; set => m_owner = value; }

        protected DataSectionPlayerBasicInfo m_basicInfoDS = new DataSectionPlayerBasicInfo();

        #region IComponent实现

        public string GetName()
        {
            return "PlayerBasicInfo";
        }

        public void Init()
        {
        }

        public void DeInit()
        {
        }

        public void PostInit()
        {
        }

        public virtual void DeSerialize<T>(T source)
        {
        }

        public void PostDeSerialize()
        {
        }

        public virtual bool Serialize<T>(T dest)
        {
            return true;
        }

        public void Tick(uint deltaMillisecond)
        {
        }

        #endregion

        #region IDataSectionOwner实现
        public void OnDataSectionSaveEnd()
        {
            throw new NotImplementedException();
        }

        public void SyncInitDataToClient(List<object> syncDestDatas)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
