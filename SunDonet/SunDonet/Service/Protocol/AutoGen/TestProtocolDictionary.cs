using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet.Protocol
{
    /// <summary>
    /// 协议字典类
    /// </summary>
    public class TestProtocolDictionary : ProtocolDictionaryBase
    {
	    /// <summary>
        /// 生成字典
        /// </summary>
        public TestProtocolDictionary()
        {
						
			m_idTypeMap[1] = typeof(ClientLoginReq);
						
			m_idTypeMap[2] = typeof(ClientLoginAck);
						
            foreach (KeyValuePair<Int32, Type> entity in m_idTypeMap)
            {
                m_typeIdMap[entity.Value] = entity.Key;
            }
        }

        		
		public const Int32 MsgId_ClientLoginReq = 1;
				
		public const Int32 MsgId_ClientLoginAck = 2;
				
    }
}
