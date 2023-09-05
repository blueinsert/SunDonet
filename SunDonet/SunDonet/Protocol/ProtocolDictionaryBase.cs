using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public abstract class ProtocolDictionaryBase
    {
        /// <summary>
        /// 字典容器
        /// </summary>
        protected Dictionary<Int32, Type> m_idTypeMap = new Dictionary<Int32, Type>();
        protected Dictionary<Type, Int32> m_typeIdMap = new Dictionary<Type, Int32>();

        /// <summary>
        /// 通过类型ID获取类型
        /// </summary>
        /// <param name="typeID">类型ID</param>
        /// <returns>类型</returns>
        virtual public Type GetTypeById(Int32 typeID)
        {
            return m_idTypeMap[typeID];
        }

        /// <summary>
        /// 通过类型获取类型ID
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>类型ID</returns>
        virtual public Int32 GetIdByType(Type type)
        {
            return m_typeIdMap[type];
        }
    }
}
