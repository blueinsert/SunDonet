using Google.Protobuf;
using SunDonet.DB;
using SunDonet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class PlayerBasicInfoComponent: PlayerBasicInfoComponentCommon, IServerComponent
    {
        public override void DeSerialize<T>(T source)
        {
            Console.WriteLine("PlayerBasicInfoComponent:DeSerialize");
            DBCollectionPlayer player = source as DBCollectionPlayer;
            if (player == null)
                return;
            var basicInfoDB = player.BasicInfo;
            m_basicInfoDS.Name = basicInfoDB.Name;
            m_basicInfoDS.PlayerLevel = basicInfoDB.PlayerLevel;
            m_basicInfoDS.Exp = basicInfoDB.Exp;
            m_basicInfoDS.Energy = basicInfoDB.Energy;
            m_basicInfoDS.Gold = basicInfoDB.Gold;
            m_basicInfoDS.InitVersion(basicInfoDB.Version);
        }

        public override bool Serialize<T>(T dest)
        {
            //Console.WriteLine("PlayerBasicInfoComponent:Serialize");
            if (!m_basicInfoDS.NeedSync2DB())
                return false;
            var updateBuilder = dest as MongoPartialDBUpdateDocumentBuilder<DBCollectionPlayer>;
            DBStructurePlayerBasicInfo dbBasicInfo = new DBStructurePlayerBasicInfo();
            dbBasicInfo.Name = m_basicInfoDS.Name;
            dbBasicInfo.PlayerLevel = m_basicInfoDS.PlayerLevel;
            dbBasicInfo.Exp = m_basicInfoDS.Exp;
            dbBasicInfo.Energy = m_basicInfoDS.Energy;
            dbBasicInfo.Gold = m_basicInfoDS.Gold;
            dbBasicInfo.Version = m_basicInfoDS.Version;
            updateBuilder.Set<DBStructurePlayerBasicInfo>((m) => m.BasicInfo, dbBasicInfo);
            return true;
        }

        #region IDataSectionOwner实现
        public void OnDataSectionSaveEnd()
        {
            m_basicInfoDS.OnDBSynced();
        }

        public void SyncInitDataToClient(List<object> syncDestDatas)
        {
            PlayerBasicInfoNtf ntf = new PlayerBasicInfoNtf();
            ntf.Name = m_basicInfoDS.Name;
            ntf.PlayerLevel = m_basicInfoDS.PlayerLevel;
            ntf.Exp = m_basicInfoDS.Exp;
            ntf.Energy = m_basicInfoDS.Energy;
            ntf.Gold = m_basicInfoDS.Gold;
            syncDestDatas.Add(ntf);
        }
        #endregion
    }
}
