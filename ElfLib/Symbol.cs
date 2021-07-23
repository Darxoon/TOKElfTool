using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ElfLib
{
    public enum SymbolVisibility : byte
    {
        Default,
        Internal,
        Hidden,
        Protected,
    }

    public class Symbol
    {

        internal Pointer internalName;

        public string Name { get; }

        public byte Info { get; set; }

        /// <summary>
        /// How and if external modules can access this symbol.
        /// Usually called `st_other`
        /// </summary>
        public byte Visibility { get; set; }

        public short SectionHeaderIndex { get; }

        public Section Section { get; set; }

        public long Value { get; set; }

        public long Size { get; set; }

        internal Symbol(BinaryReader reader, Section stringTable, List<Section> sections)
        {
            internalName = new Pointer(reader.ReadInt32());
            Name = stringTable.GetString(internalName);
            Info = reader.ReadByte();
            Visibility = reader.ReadByte();
            SectionHeaderIndex = reader.ReadInt16();
            try
            {
                Section = sections[SectionHeaderIndex];
            } catch (ArgumentOutOfRangeException)
            {
                Section = null;
            }
            Value = reader.ReadInt64();
            Size = reader.ReadInt64();
        }

        internal void ToBinaryWriter(BinaryWriter writer)
        {
            writer.Write(internalName.AsInt);
            writer.Write(Info);
            writer.Write((byte)Visibility);
            writer.Write(SectionHeaderIndex);
            writer.Write(Value);
            writer.Write(Size);
        }
    }
}
