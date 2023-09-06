using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class DataSectionPlayerBasicInfo:DataSectionBase
    {
        public string Name
        {
            get;
            set;
        }

        public Int32 PlayerLevel
        {
            get;
            set;
        }

        public Int32 Exp
        {
            get;
            set;
        }

        /// <summary>
        /// 体力
        /// </summary>
        public Int32 Energy
        {
            get;
            set;
        }

        public Int32 Gold
        {
            get;
            set;
        }

        public override object SerializeToClient()
        {
            return null;//todo
        }
    }
}
