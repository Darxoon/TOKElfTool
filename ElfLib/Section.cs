using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ElfLib
{
    public sealed class Section
    {
        internal ElfStringPointer namePointer;

        public string Name { get; set; } = "";
        public int Type { get; set; }
        public long Flags { get; set; }
        public long Addr { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public int Link { get; set; }
        public int Info { get; set; }
        public long AddrAlign { get; set; }
        public long EntSize { get; set; }

        public byte[] Content { get; set; }

        public string GetString(int offset)
        {
            if (offset == int.MaxValue)
                return null;

            int pos = offset;
            byte c;
            List<byte> list = new List<byte>();
            do
            {
                c = Content[pos];
                list.Add(c);
                pos += 1;
            } while (c != '\0');

            string outputWithNull = Encoding.UTF8.GetString(list.ToArray());
            string output = outputWithNull.Substring(0, outputWithNull.Length - 1);
            return output;
        }

        /// <summary>
        /// Creates a new binary reader for the section's content.
        /// IMPORTANT NOTE: It has to be disposed, either manually or with `using`
        /// </summary>
        public BinaryReader CreateBinaryReader() =>
            new BinaryReader(new MemoryStream(Content));

        public Section Clone(byte[] newContent = null, long? newOffset = null)
        {
            Section clone = (Section)MemberwiseClone();
            if (newContent != null)
            {
                clone.Content = newContent;
                clone.Size = newContent.Length;
            }
            if (newOffset != null)
                clone.Offset = (long)newOffset;
            return clone;
        }

        internal void ToBinaryWriter(BinaryWriter writer)
        {
            writer.Write(namePointer.AsInt);
            writer.Write(Type);
            writer.Write(Flags);
            writer.Write(Addr);
            writer.Write(Offset);
            writer.Write(Size);
            writer.Write(Link);
            writer.Write(Info);
            writer.Write(AddrAlign);
            writer.Write(EntSize);
        }

        public string GetString(ElfStringPointer pointer)
        {
            return GetString(pointer.AsInt);
        }

        public override string ToString()
        {
            return $"Section{{{(Name.StartsWith("_UNAVAILABLE") ? "" : Name.PadRight(16, ' ') + ": ")}Type {Type:2}, Offset 0x{Offset:X4}, 0x{Size:X4} bytes long}}";
        }

        internal Section()
        {

        }
        internal Section(BinaryReader reader)
        {
            namePointer = new ElfStringPointer(reader.ReadInt32());
            Name = $"_UNAVAILABLE_{namePointer.AsLong:X4}";
            Type = reader.ReadInt32();
            Flags = reader.ReadInt64();
            Addr = reader.ReadInt64();
            Offset = reader.ReadInt64();
            Size = reader.ReadInt64();
            Link = reader.ReadInt32();
            Info = reader.ReadInt32();
            AddrAlign = reader.ReadInt64();
            EntSize = reader.ReadInt64();

            Content = new byte[8];
        }
    }
}
