using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet.Protocol
{
    public interface IProtoProvider
    {
        /// Query message type by message id
        Type GetTypeById(int vId);

        /// Query message id by message type
        int GetIdByType(Type vType);
    }
}
