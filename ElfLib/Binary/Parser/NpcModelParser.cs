using System.Collections.Generic;
using System.Diagnostics;
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

            Trace.WriteLine("Parsing .data");
            var dataParser = new StringDataParser<NpcModel, RawNpcModel>(stringSection, 
                new SimpleDataParser<RawNpcModel>(dataSection, dataRelocationTable, out var mainDataOffsets));

            List<object> dataModels = dataParser.Parse()[ElfType.Main];
            dataOffsets[ElfType.Main] = mainDataOffsets[ElfType.Main];
            
            // Collect references to .rodata

            Trace.WriteLine("Collecting references to Model Files and State");
            List<long> modelFilesOffsets = new List<long>();
            List<int> modelFilesLengths = new List<int>();
            List<long> stateOffsets = new List<long>();
            List<int> stateLengths = new List<int>();

            for (int i = 0; i < dataModels.Count; i++)
            {
                NpcModel model = (NpcModel)dataModels[i];
                modelFilesOffsets.Add(model.model_files_ptr.AsLong);
                modelFilesLengths.Add(model.model_files_count);
                stateOffsets.Add(model.state_ptr.AsLong);
                stateLengths.Add(model.state_count);
            }
            
            // Parse .rodata section

            Trace.WriteLine("Parsing .rodata");
            var filesParser = new StringDataParser<NpcModelFiles,RawNpcModelFiles>(stringSection,
                new NpcModelPartParser<RawNpcModelFiles>(rodataSection, modelFilesOffsets, modelFilesLengths, rodataRelocationTable));
            
            var stateParser = new StringDataParser<NpcModelState,RawNpcModelState>(stringSection, 
                new NpcModelPartParser<RawNpcModelState>(rodataSection, stateOffsets, stateLengths, rodataRelocationTable));

            var filesData = filesParser.Parse()[ElfType.Main];
            var stateData = stateParser.Parse()[ElfType.Main];
            
            dataOffsets[ElfType.Files] = modelFilesOffsets;
            dataOffsets[ElfType.State] = stateOffsets;
            
            // Collect references to substate objects

            Trace.WriteLine("Collecting references to substate objects");
            List<long> substateOffsets = new List<long>();
            List<int> substateCounts = new List<int>();
            for (int i = 0; i < stateData.Count; i++)
            {
                List<object> stateArr = (List<object>)stateData[i];
                for (int j = 0; j < stateArr.Count; j++)
                {
                    NpcModelState state = (NpcModelState)stateArr[j];
                    substateOffsets.Add(state.substate_arr.AsLong);
                    substateCounts.Add(state.substate_count);
                }
            }
            
            // Parse substate
            
            Trace.WriteLine("Parsing substate objects");
            var substateParser = new StringDataParser<NpcModelSubState,RawNpcModelSubState>(stringSection,
                new NpcModelPartParser<RawNpcModelSubState>(rodataSection, substateOffsets, substateCounts, rodataRelocationTable));

            var substateData = substateParser.Parse()[ElfType.Main];
            
            dataOffsets[ElfType.SubStates] = substateOffsets;
            
            return new SortedDictionary<ElfType, List<object>>
            {
                {ElfType.Main, dataModels},
                {ElfType.Files, filesData},
                {ElfType.State, stateData},
                {ElfType.SubStates, substateData},
            };
        }
    }
}