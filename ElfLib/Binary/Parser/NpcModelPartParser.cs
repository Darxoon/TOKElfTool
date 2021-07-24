using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ElfLib.Binary.Parser
{
    internal class NpcModelPartParser<T> : IDataParser where T : struct
    {
        private readonly Section section;
        private readonly List<long> partOffsets;
        private readonly List<int> partCounts;
        private readonly List<SectionRela> relocationTable;

        public NpcModelPartParser(
            Section section, List<long> partOffsets, List<int> partCounts, 
            List<SectionRela> relocationTable)
        {
            this.section = section;
            this.partOffsets = partOffsets;
            this.partCounts = partCounts;
            this.relocationTable = relocationTable;
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            using MemoryStream stream = new MemoryStream(section.Content);
            using BinaryReader reader = new BinaryReader(stream);

            List<T[]> instances = new List<T[]>();
            
            for (int i = 0; i < partOffsets.Count; i++)
            {
                stream.Position = partOffsets[i];
                
                T[] partInstances = new T[partCounts[i]];
                for (int j = 0; j < partCounts[i]; j++)
                    partInstances[j] = SimpleDataParser<T>.FromBinaryReader(reader);
                
                instances.Add(partInstances);
            }
            
            List<object[]> boxedInstances = instances.Select(arr => arr.Cast<object>().ToArray()).ToList();

            Trace.WriteLine("Resolving relocations");
            ResolveRelocations(boxedInstances);

            return new Dictionary<ElfType, List<object>>
            {
                {ElfType.Main, boxedInstances.Cast<object>().ToList()},
            };
        }
        
        private void ResolveRelocations(List<object[]> objects)
        {
            int size = Marshal.SizeOf(typeof(T));

            foreach (SectionRela relocation in relocationTable)
            {
                for (int i = 0; i < partOffsets.Count; i++)
                {
                    if (relocation.OriginOffset < partOffsets[i] + size * partCounts[i])
                    {
                        long fieldOffset = relocation.OriginOffset - partOffsets[i];
                        
                        // iterate through all fields and apply the relocation
                        foreach (FieldInfo field in typeof(T).GetFields())
                        {
                            if (field.GetFieldOffset() == fieldOffset)
                            {
                                if (field.FieldType == typeof(Pointer))
                                    field.SetValue(objects[i][(relocation.OriginOffset - partOffsets[i]) / size], new Pointer(relocation.Addend));
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