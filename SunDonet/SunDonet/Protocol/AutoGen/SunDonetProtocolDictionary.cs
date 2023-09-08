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
    public class SunDonetProtocolDictionary : ProtocolDictionaryBase
    {
	    /// <summary>
        /// 生成字典
        /// </summary>
        public SunDonetProtocolDictionary()
        {
						
			m_idTypeMap[1] = typeof(LoginReq);
						
			m_idTypeMap[2] = typeof(LoginAck);
						
			m_idTypeMap[3] = typeof(CreateAccountReq);
						
			m_idTypeMap[4] = typeof(CreateAccountAck);
						
			m_idTypeMap[5] = typeof(PlayerInfoInitReq);
						
			m_idTypeMap[6] = typeof(PlayerInfoInitAck);
						
			m_idTypeMap[7] = typeof(PlayerInfoInitEndNtf);
						
			m_idTypeMap[8] = typeof(PlayerBasicInfoNtf);
						
            foreach (KeyValuePair<Int32, Type> entity in m_idTypeMap)
            {
                m_typeIdMap[entity.Value] = entity.Key;
            }
        }

        		
		public const Int32 MsgId_LoginReq = 1;
				
		public const Int32 MsgId_LoginAck = 2;
				
		public const Int32 MsgId_CreateAccountReq = 3;
				
		public const Int32 MsgId_CreateAccountAck = 4;
				
		public const Int32 MsgId_PlayerInfoInitReq = 5;
				
		public const Int32 MsgId_PlayerInfoInitAck = 6;
				
		public const Int32 MsgId_PlayerInfoInitEndNtf = 7;
				
		public const Int32 MsgId_PlayerBasicInfoNtf = 8;
				
    }
}
