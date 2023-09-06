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
        public IComponentOwner Owner { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected DataSectionPlayerBasicInfo m_basicInfoDS = new DataSectionPlayerBasicInfo();

        #region IComponent实现

        public string GetName()
        {
            throw new NotImplementedException();
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
