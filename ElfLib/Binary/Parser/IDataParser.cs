using System;
using System.Collections.Generic;
using System.Text;

namespace ElfLib.Binary.Parser
{
    interface IDataParser
    {
        public IDictionary<ElfType, List<object>> Parse();
    }
}
