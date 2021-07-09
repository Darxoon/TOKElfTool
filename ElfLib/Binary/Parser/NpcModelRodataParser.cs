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
        private readonly Dictionary<ElfType,List<long>> dataOffsets;

        public NpcModelRodataParser(Section section, Section stringSection, List<long> modelFilesOffsets,
            List<long> modelStateOffsets, List<SectionRela> relocationTable, out Dictionary<ElfType,List<long>> dataOffsets)
        {
            this.section = section;
            this.stringSection = stringSection;
            this.modelFilesOffsets = modelFilesOffsets;
            this.modelStateOffsets = modelStateOffsets;
            this.relocationTable = relocationTable;

            this.dataOffsets = dataOffsets = new Dictionary<ElfType,List<long>>();
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            using MemoryStream stream = new MemoryStream(section.Content);
            using BinaryReader reader = new BinaryReader(stream);

            Dictionary<ElfType, List<long>> filesOffsets;
            Dictionary<ElfType, List<long>> stateOffsets;
                
            var filesParser = new StringDataParser<NpcModelFiles,RawNpcModelFiles>(stringSection,
                new NpcModelPartParser<RawNpcModelFiles>(section, modelFilesOffsets, relocationTable, out filesOffsets));
            
            var stateParser = new StringDataParser<NpcModelState,RawNpcModelState>(stringSection, 
                new NpcModelPartParser<RawNpcModelState>(section, modelStateOffsets, relocationTable, out stateOffsets));

            var filesData = filesParser.Parse()[ElfType.Main];
            var stateData = stateParser.Parse()[ElfType.Main];

            dataOffsets[ElfType.Files] = filesOffsets[ElfType.Main];
            dataOffsets[ElfType.State] = stateOffsets[ElfType.Main];
            
            return new Dictionary<ElfType, List<object>>
            {
                {ElfType.Files, filesData},
                {ElfType.State, stateData},
            };
        }
    }
}
