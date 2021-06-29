using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElfLib.CustomDataTypes;
using ElfLib.CustomDataTypes.Registry;

namespace ElfLib.Binary.Parser
{
    internal class SimpleDataParser<T> : IDataParser
    {
        private readonly Section dataSection;
        private readonly List<SectionRela> relocationTable;
        private readonly FromBinaryDataConverter converter;
        private readonly long startOffset;
        private readonly int amount;

        public delegate T FromBinaryDataConverter(BinaryReader binaryReader, List<SectionRela> relocationTable, long baseOffset);

        public SimpleDataParser(Section dataSection, List<SectionRela> relocationTable, FromBinaryDataConverter converter, long startOffset = 0, int amount = -1)
        {
            this.dataSection = dataSection;
            this.relocationTable = relocationTable;
            this.converter = converter;
            this.startOffset = startOffset;
            this.amount = amount;
        }

        public Dictionary<ElfType, List<object>> Parse()
        {
            List<object> objects = new List<object>();

            MemoryStream stream = new MemoryStream(dataSection.Content);
            stream.Position = startOffset;
            
            BinaryReader reader = new BinaryReader(stream);

            if (amount == -1)
            {
                while (stream.Position != stream.Length)
                {
                    objects.Add(converter(reader, relocationTable, reader.BaseStream.Position));
                }
            }
            else
            {
                for (int i = 0; i < amount && stream.Position != stream.Length; i++)
                {
                    objects.Add(converter(reader, relocationTable, reader.BaseStream.Position));
                }
            }

            return new Dictionary<ElfType, List<object>>
            {
                {ElfType.Main, objects},
            };
        }

    }
}
