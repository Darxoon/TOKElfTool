using System.Collections.Generic;
using System.Linq;
using ElfLib.CustomDataTypes;

namespace ElfLib.Binary.Parser
{
    internal class NpcModelParser : IDataParser
    {
        private readonly Section stringSection;
        private readonly Section dataSection;
        private readonly Section rodataSection;
        
        private readonly List<SectionRela> relocationTable;

        public NpcModelParser(Section stringSection, Section dataSection, Section rodataSection, List<SectionRela> relocationTable)
        {
            this.stringSection = stringSection;
            this.dataSection = dataSection;
            this.rodataSection = rodataSection;
            this.relocationTable = relocationTable;
        }

        public IDictionary<ElfType, List<object>> Parse()
        {

        }
    }
}