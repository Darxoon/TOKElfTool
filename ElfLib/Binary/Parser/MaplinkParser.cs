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
        private readonly Dictionary<ElfType,List<long>> dataOffsets;

        public MaplinkParser(List<Symbol> symbolTable, Section dataSection, Section stringSection, List<SectionRela> relocationTable,
            out Dictionary<ElfType, List<long>> dataOffsets)
        {
            this.symbolTable = symbolTable;
            this.dataSection = dataSection;
            this.stringSection = stringSection;
            this.relocationTable = relocationTable;

            this.dataOffsets = dataOffsets = new Dictionary<ElfType, List<long>>();
        }
        
        public IDictionary<ElfType, List<object>> Parse()
        {
            List<Symbol> symbols = symbolTable.Where((symbol, index) => index > 5 && symbol.Section == dataSection).OrderBy(symbol => symbol.Value).ToList();
            int nodeAmount = (int)(symbols[0].Size / Marshal.SizeOf(typeof(RawMaplinkNode)));

            Dictionary<ElfType, List<long>> nodeOffsets;
            Dictionary<ElfType, List<long>> headerOffsets;

            List<SectionRela> nodeRelocations = relocationTable.Take(relocationTable.Count - 2).ToList();
            var nodeParser = new StringDataParser<MaplinkNode, RawMaplinkNode>(stringSection,
                new SimpleDataParser<RawMaplinkNode>(dataSection, nodeRelocations, out nodeOffsets, 0, nodeAmount));

            List<SectionRela> headerRelocations = relocationTable.Skip(relocationTable.Count - 2).ToList();
            var headerParser = new StringDataParser<MaplinkHeader, RawMaplinkHeader>(stringSection,
                new SimpleDataParser<RawMaplinkHeader>(dataSection, headerRelocations, out headerOffsets, symbols[1].Value));

            List<object> nodes = nodeParser.Parse()[ElfType.Main];
            List<object> headers = headerParser.Parse()[ElfType.Main];

            dataOffsets[ElfType.Main] = nodeOffsets[ElfType.Main];
            dataOffsets[ElfType.MaplinkHeader] = headerOffsets[ElfType.Main];
            
            return new Dictionary<ElfType, List<object>>
            {
                {ElfType.Main, nodes},
                {ElfType.MaplinkHeader, headers},
            };
        }
    }
}