using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ElfLib.CustomDataTypes;

namespace ElfLib.Binary.Parser
{
    internal class MaplinkParser : IDataParser
    {
        private readonly List<Symbol> symbolTable;
        private readonly Section dataSection;
        private readonly Section stringSection;
        private readonly List<SectionRela> relocationTable;

        public MaplinkParser(List<Symbol> symbolTable, Section dataSection, Section stringSection, List<SectionRela> relocationTable)
        {
            this.symbolTable = symbolTable;
            this.dataSection = dataSection;
            this.stringSection = stringSection;
            this.relocationTable = relocationTable;
        }
        
        public IDictionary<ElfType, List<object>> Parse()
        {
            List<Symbol> symbols = symbolTable.Where((symbol, index) => index > 5 && symbol.Section == dataSection).OrderBy(symbol => symbol.Value).ToList();
            int nodeAmount = (int)(symbols[0].Size / Marshal.SizeOf(typeof(RawMaplinkNode)));

            var nodeParser = new StringDataParser<MaplinkNode, RawMaplinkNode>(stringSection,
                new SimpleDataParser<RawMaplinkNode>(dataSection, relocationTable.Take(relocationTable.Count - 2).ToList(), 0, nodeAmount));

            List<SectionRela> x = relocationTable.Skip(relocationTable.Count - 2).ToList();
            var headerParser = new StringDataParser<MaplinkHeader, RawMaplinkHeader>(stringSection,
                new SimpleDataParser<RawMaplinkHeader>(dataSection, x, symbols[1].Value));
            
            return new Dictionary<ElfType, List<object>>
            {
                {ElfType.Main, nodeParser.Parse()[ElfType.Main]},
                {ElfType.MaplinkHeader, headerParser.Parse()[ElfType.Main]},
            };
        }
    }
}