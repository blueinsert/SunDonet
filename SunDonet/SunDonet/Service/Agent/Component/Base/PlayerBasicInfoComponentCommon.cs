using MongoDB.Driver;
using SunDonet.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class PlayerBasicInfoComponentCommon : IComponent
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
            SunNet.Instance.Log.Info("PlayerBasicInfoComponentCommon:Init");
        }

        public void DeInit()
        {
            SunNet.Instance.Log.Info("PlayerBasicInfoComponentCommon:Init");
        }

        public void PostInit()
        {
            SunNet.Instance.Log.Info("PlayerBasicInfoComponentCommon:PostInit");
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

    }
}
