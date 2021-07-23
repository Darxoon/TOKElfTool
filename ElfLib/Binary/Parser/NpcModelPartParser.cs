using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ElfLib.CustomDataTypes;

namespace ElfLib.Binary.Parser
{
    internal class NpcModelPartParser<T> : IDataParser
    {
        private readonly Section section;
        private readonly List<long> partOffsets;
        private readonly List<SectionRela> relocationTable;
        private readonly Dictionary<ElfType,List<long>> dataOffsets;

        public NpcModelPartParser(Section section, List<long> partOffsets, List<SectionRela> relocationTable, out Dictionary<ElfType,List<long>> dataOffsets)
        {
            this.section = section;
            this.partOffsets = partOffsets;
            this.relocationTable = relocationTable;

            this.dataOffsets = dataOffsets = new Dictionary<ElfType,List<long>>();
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            using MemoryStream stream = new MemoryStream(section.Content);
            using BinaryReader reader = new BinaryReader(stream);

            List<object> instances = new List<object>();

            dataOffsets[ElfType.Main] = new List<long>();
            
            for (int i = 0; i < partOffsets.Count; i++)
            {
                stream.Position = partOffsets[i];
                dataOffsets[ElfType.Main].Add(partOffsets[i]);
                instances.Add(SimpleDataParser<T>.FromBinaryReader(reader));
            }
            
            ResolveRelocations(instances);

            return new Dictionary<ElfType, List<object>>
            {
                {ElfType.Main, instances},
            };
        }
        
        private void ResolveRelocations(List<object> objects)
        {
            int size = Marshal.SizeOf(typeof(T));

            foreach (SectionRela relocation in relocationTable)
            {
                for (int i = 0; i < partOffsets.Count; i++)
                {
                    if (relocation.OriginOffset < partOffsets[i] + size)
                    {
                        long fieldOffset = relocation.OriginOffset - partOffsets[i];
                        
                        // iterate through all fields and apply the relocation
                        foreach (FieldInfo field in typeof(T).GetFields())
                        {
                            if (field.GetFieldOffset() == fieldOffset)
                            {
                                if (field.FieldType == typeof(Pointer))
                                    field.SetValue(objects[i], new Pointer(relocation.Addend));
                                else
                                    Trace.WriteLine($"Possible unidentified string: {typeof(T)} {field}");
                                break;
                            }
                        }
                    }
                }
                
            }
        }
    }
}