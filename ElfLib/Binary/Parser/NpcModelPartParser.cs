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

        public NpcModelPartParser(Section section, List<long> partOffsets, List<SectionRela> relocationTable)
        {
            this.section = section;
            this.partOffsets = partOffsets;
            this.relocationTable = relocationTable;
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            using MemoryStream stream = new MemoryStream(section.Content);
            using BinaryReader reader = new BinaryReader(stream);

            List<object> instances = new List<object>();

            for (int i = 0; i < partOffsets.Count; i++)
            {
                stream.Position = partOffsets[i];
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
                                if (field.FieldType == typeof(ElfStringPointer))
                                    field.SetValue(objects[i], new ElfStringPointer(relocation.Addend));
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