using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib.Binary.Parser
{
    internal class SimpleDataParser<T> : IDataParser
    {
        private readonly Section dataSection;
        private readonly List<SectionRela> relocationTable;
        private readonly long startOffset;
        private readonly int amount;

        private Dictionary<ElfType, List<long>> dataOffsets;

        public SimpleDataParser(Section dataSection, List<SectionRela> relocationTable, out Dictionary<ElfType, List<long>> dataOffsets, long startOffset = 0, int amount = -1)
        {
            this.dataSection = dataSection;
            this.relocationTable = relocationTable;
            this.startOffset = startOffset;
            this.amount = amount;

            this.dataOffsets = dataOffsets = new Dictionary<ElfType, List<long>>();
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            List<object> objects = new List<object>();

            MemoryStream stream = new MemoryStream(dataSection.Content);
            stream.Position = startOffset;
            
            BinaryReader reader = new BinaryReader(stream);

            dataOffsets[ElfType.Main] = new List<long>();
            
            if (amount == -1)
            {
                while (stream.Position != stream.Length)
                {
                    dataOffsets[ElfType.Main].Add(stream.Position);
                    objects.Add(FromBinaryReader(reader));
                }
            }
            else
            {
                for (int i = 0; i < amount && stream.Position != stream.Length; i++)
                {
                    dataOffsets[ElfType.Main].Add(stream.Position);
                    objects.Add(FromBinaryReader(reader));
                }
            }

            ResolveRelocations(objects);

            return new Dictionary<ElfType, List<object>>
            {
                {ElfType.Main, objects},
            };
        }

        internal static T FromBinaryReader(BinaryReader reader)
        {
            object result = Util.FromBinaryReader<T>(reader);
            FieldInfo[] fields = typeof(T).GetFields();
            
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(Pointer))
                    fields[i].SetValue(result, Pointer.NULL);
            }

            return (T)result;
        }
        
        private void ResolveRelocations(List<object> objects)
        {
            int size = Marshal.SizeOf(typeof(T));

            foreach (SectionRela relocation in relocationTable)
            {
                long offset = relocation.OriginOffset - startOffset;
                
                long instanceIndex = offset / size;
                // instanceIndex is getting floored, which is why instanceIndex * size is the offset of the instance
                long fieldOffset = relocation.OriginOffset - instanceIndex * size - startOffset;
                
                // iterate through all fields and apply the relocation
                foreach (FieldInfo field in typeof(T).GetFields())
                {
                    if (field.GetFieldOffset() == fieldOffset)
                    {
                        if (field.FieldType == typeof(Pointer))
                            field.SetValue(objects[(int)instanceIndex], new Pointer(relocation.Addend));
                        else
                            Trace.WriteLine($"Possible unidentified string: {field}");
                        break;
                    }
                }
            }
        }
        
    }
}
