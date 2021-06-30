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
            // Parse .data section
            var dataParser = new StringDataParser<NpcModel, RawNpcModel>(stringSection, 
                new SimpleDataParser<RawNpcModel>(dataSection, relocationTable));

            List<object> dataModels = dataParser.Parse()[ElfType.Main];
            
            // Collect references to .rodata
            List<long> modelFilesOffsets = new List<long>();
            List<long> modelStateOffsets = new List<long>();

            for (int i = 0; i < dataModels.Count; i++)
            {
                NpcModel model = (NpcModel)dataModels[i];
                modelFilesOffsets.Add(model.model_files_ptr);
                modelStateOffsets.Add(model.state_ptr);
            }
            
            // Parse .rodata section
            var rodataParser = new NpcModelRodataParser(rodataSection, stringSection, modelFilesOffsets, modelStateOffsets, relocationTable);
            
            IDictionary<ElfType, List<object>> rodataObjects = rodataParser.Parse();
            
            return new SortedDictionary<ElfType, List<object>>
            {
                {ElfType.Main, dataModels},
                {ElfType.Files, rodataObjects[ElfType.Files]},
                {ElfType.State, rodataObjects[ElfType.State]},
            };
        }
    }
}