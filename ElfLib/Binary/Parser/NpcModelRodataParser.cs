using System.Collections.Generic;
using System.IO;
using ElfLib.CustomDataTypes;

namespace ElfLib.Binary.Parser
{
    internal class NpcModelRodataParser : IDataParser
    {
        private readonly Section section;
        private readonly Section stringSection;
        private readonly List<long> modelFilesOffsets;
        private readonly List<long> modelStateOffsets;
        private readonly List<SectionRela> relocationTable;
        
        private readonly SortedDictionary<long, object> additionalPositionalData;

        public NpcModelRodataParser(Section section, Section stringSection, List<long> modelFilesOffsets,
            List<long> modelStateOffsets, List<SectionRela> relocationTable,
            SortedDictionary<long, object> additionalPositionalData)
        {
            this.section = section;
            this.stringSection = stringSection;
            this.modelFilesOffsets = modelFilesOffsets;
            this.modelStateOffsets = modelStateOffsets;
            this.relocationTable = relocationTable;
            this.additionalPositionalData = additionalPositionalData;
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            using MemoryStream stream = new MemoryStream(section.Content);
            using BinaryReader reader = new BinaryReader(stream);

            var filesParser = new NpcModelStringPartParser<NpcModelFiles, RawNpcModelFiles>(stringSection, additionalPositionalData,
                new NpcModelPartParser<RawNpcModelFiles>(additionalPositionalData, section, modelFilesOffsets, relocationTable));
            
            var stateParser = new NpcModelStringPartParser<NpcModelState, RawNpcModelState>(stringSection, additionalPositionalData, 
                new NpcModelPartParser<RawNpcModelState>(additionalPositionalData, section, modelStateOffsets, relocationTable));

            return new Dictionary<ElfType, List<object>>
            {
                {ElfType.Files, filesParser.Parse()[ElfType.Main]},
                {ElfType.State, stateParser.Parse()[ElfType.Main]},
            };
        }
    }
}
