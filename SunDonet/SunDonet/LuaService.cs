using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniLua;

namespace SunDonet
{
    //todo
    public class LuaService : ServiceBase
    {
        public LuaService(int id) : base(id) { }

        ILuaState m_luaState = null;

        public override void OnInit()
        {
            base.OnInit();
            m_luaState = new LuaState();
        }
    }
}
