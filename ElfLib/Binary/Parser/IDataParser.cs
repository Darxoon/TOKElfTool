using System;
using System.Collections.Generic;
using System.Text;

namespace ElfLib.Binary.Parser
{
    interface IDataParser
    {
        public Dictionary<ElfType, List<object>> Parse();
    }
}
