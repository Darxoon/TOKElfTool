using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public struct RawMaplinkNode
    {
        public ElfStringPointer field_0x0;
        public ElfStringPointer field_0x8;
        public ElfStringPointer field_0x10;
        public ElfStringPointer field_0x18;
        public ElfStringPointer field_0x20;
        public ElfStringPointer field_0x28;
        public float field_0x30;
        public int field_0x34;
        public int field_0x38;
        public int field_0x3c;
        public int field_0x40;
        public int field_0x44;
        public int field_0x48;
        public int field_0x4c;
        public ElfStringPointer field_0x50;
        public int field_0x58;
        public int field_0x5c;
        public ElfStringPointer field_0x60;
        public int field_0x68;
        public int field_0x6c;
        public int field_0x70;
        public int field_0x74;
        public int field_0x78;
        public int field_0x7c;
        public int field_0x80;
        public int field_0x84;
        public int field_0x88;
        public int field_0x8c;
        public ElfStringPointer field_0x90;
        public ElfStringPointer field_0x98;
        public int field_0xa0;
        public int field_0xa4;
        public int field_0xa8;
        public int field_0xac;

        public static RawMaplinkNode From(MaplinkNode item, Dictionary<string, ElfStringPointer> stringSectionTable, SortedDictionary<long, ElfStringPointer> stringRelocTable = null, long baseOffset = 0)
            => Util.NormalToRawObject<RawMaplinkNode, MaplinkNode>(item, stringSectionTable, stringRelocTable, baseOffset);

        internal static RawMaplinkNode ReadBinaryData(BinaryReader binaryReader, List<SectionRela> relas, long baseOffset)
        {
            RawMaplinkNode rawItem = Util.FromBinaryReader<RawMaplinkNode>(binaryReader);
            rawItem.field_0x0 = ElfStringPointer.ResolveRelocation(relas, 0, baseOffset);
            rawItem.field_0x8 = ElfStringPointer.ResolveRelocation(relas, 8, baseOffset);
            rawItem.field_0x10 = ElfStringPointer.ResolveRelocation(relas, 16, baseOffset);
            rawItem.field_0x18 = ElfStringPointer.ResolveRelocation(relas, 24, baseOffset);
            rawItem.field_0x20 = ElfStringPointer.ResolveRelocation(relas, 32, baseOffset);
            rawItem.field_0x28 = ElfStringPointer.ResolveRelocation(relas, 40, baseOffset);
            rawItem.field_0x50 = ElfStringPointer.ResolveRelocation(relas, 80, baseOffset);
            rawItem.field_0x60 = ElfStringPointer.ResolveRelocation(relas, 96, baseOffset);
            rawItem.field_0x90 = ElfStringPointer.ResolveRelocation(relas, 144, baseOffset);
            rawItem.field_0x98 = ElfStringPointer.ResolveRelocation(relas, 152, baseOffset);

            return rawItem;
        }
    }
}
