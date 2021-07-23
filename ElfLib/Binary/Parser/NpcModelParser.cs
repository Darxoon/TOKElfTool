using System.Collections.Generic;
using System.Linq;
using ElfLib.Types.Registry;

namespace ElfLib.Binary.Parser
{
    internal class NpcModelParser : IDataParser
    {
        private readonly Section stringSection;
        private readonly Section dataSection;
        private readonly Section rodataSection;
        
        private readonly List<SectionRela> dataRelocationTable;
        private readonly List<SectionRela> rodataRelocationTable;
        private readonly Dictionary<ElfType,List<long>> dataOffsets;

        public NpcModelParser(Section stringSection, Section dataSection, Section rodataSection, out Dictionary<ElfType,List<long>> dataOffsets,
            List<SectionRela> dataRelocationTable, List<SectionRela> rodataRelocationTable)
        {
            this.stringSection = stringSection;
            this.dataSection = dataSection;
            this.rodataSection = rodataSection;
            this.dataRelocationTable = dataRelocationTable;
            this.rodataRelocationTable = rodataRelocationTable;

            this.dataOffsets = dataOffsets = new Dictionary<ElfType, List<long>>();
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            // Parse .data section
            var dataParser = new StringDataParser<NpcModel, RawNpcModel>(stringSection, 
                new SimpleDataParser<RawNpcModel>(dataSection, dataRelocationTable, out var mainDataOffsets));

            List<object> dataModels = dataParser.Parse()[ElfType.Main];
            dataOffsets[ElfType.Main] = mainDataOffsets[ElfType.Main];
            
            // Collect references to .rodata
            List<long> modelFilesOffsets = new List<long>();
            List<long> modelStateOffsets = new List<long>();

            for (int i = 0; i < dataModels.Count; i++)
            {
                NpcModel model = (NpcModel)dataModels[i];
                modelFilesOffsets.Add(model.model_files_ptr.AsLong);
                modelStateOffsets.Add(model.state_ptr.AsLong);
            }
            
            // Parse .rodata section
            var rodataParser = new NpcModelRodataParser(rodataSection, stringSection, modelFilesOffsets, 
                modelStateOffsets, rodataRelocationTable, out var rodataOffsets);

            IDictionary<ElfType, List<object>> rodataObjects = rodataParser.Parse();
            
            dataOffsets[ElfType.Files] = rodataOffsets[ElfType.Files];
            dataOffsets[ElfType.State] = rodataOffsets[ElfType.State];
            
            return new SortedDictionary<ElfType, List<object>>
            {
                {ElfType.Main, dataModels},
                {ElfType.Files, rodataObjects[ElfType.Files]},
                {ElfType.State, rodataObjects[ElfType.State]},
            };
        }
    }
}