using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ElfLib
{
    internal class SectionRela
    {
        public const long DEFAULT_INFO = 0x600000101;

        public long Offset { get; set; }
        public long Info { get; set; }
        public long Addend { get; set; }

        public long OriginOffset
        {
            get => Offset;
            set => Offset = value;
        }

        public ElfStringPointer TargetOffsetPointer
        {
            get => new ElfStringPointer(Addend);
            set => Addend = value.AsLong;
        }
        
        private SectionRela() { }
        
        internal SectionRela(long originOffset, long info, ElfStringPointer targetOffsetPointer)
        {
            Offset = originOffset;
            Info = info;
            Addend = targetOffsetPointer.AsLong;
        }
        private SectionRela(long offset, long info, long addend)
        {
            Offset = offset;
            Info = info;
            Addend = addend;
        }

        public override string ToString()
        {
            return $"SectionRelA{{0x{Offset:X2}{(Info != DEFAULT_INFO ? ":0x" + Info.ToString("X2") : ":")}:{(Addend == int.MaxValue ? "null" : "0x" + Addend.ToString("X2"))}}}";
        }

        internal static SectionRela FromBinary(BinaryReader reader)
        {
            SectionRela rela = new SectionRela(
                reader.ReadInt64(),
                reader.ReadInt64(),
                reader.ReadInt64()
            );
            return rela;
        }
        public void ToBinaryWriter(BinaryWriter writer)
        {
            writer.Write(Offset);
            writer.Write(Info);
            writer.Write(Addend);
        }
    }
}
