using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [StructLayout(LayoutKind.Sequential)]
    public struct RawMaplinkNode
    {
        public ElfStringPointer level_str;
        public ElfStringPointer field_0x8;
        public ElfStringPointer destination_str;
        public ElfStringPointer field_0x18;
        public ElfStringPointer shape_str;
        public ElfStringPointer target_str;
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
        public ElfStringPointer direction_str;
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
        public ElfStringPointer enter_event_str;
        public ElfStringPointer exit_event_str;
        public int field_0xa0;
        public int field_0xa4;
        public int field_0xa8;
        public int field_0xac;

        public static RawMaplinkNode From(MaplinkNode item, Dictionary<string, ElfStringPointer> stringSectionTable, SortedDictionary<long, ElfStringPointer> stringRelocTable = null, long baseOffset = 0)
            => Util.NormalToRawObject<RawMaplinkNode, MaplinkNode>(item, stringSectionTable, stringRelocTable, baseOffset);

        internal static RawMaplinkNode ReadBinaryData(BinaryReader binaryReader, List<SectionRela> relas, long baseOffset)
        {
            RawMaplinkNode rawItem = Util.FromBinaryReader<RawMaplinkNode>(binaryReader);
            rawItem.level_str = ElfStringPointer.ResolveRelocation(relas, 0, baseOffset);
            rawItem.field_0x8 = ElfStringPointer.ResolveRelocation(relas, 8, baseOffset);
            rawItem.destination_str = ElfStringPointer.ResolveRelocation(relas, 16, baseOffset);
            rawItem.field_0x18 = ElfStringPointer.ResolveRelocation(relas, 24, baseOffset);
            rawItem.shape_str = ElfStringPointer.ResolveRelocation(relas, 32, baseOffset);
            rawItem.target_str = ElfStringPointer.ResolveRelocation(relas, 40, baseOffset);
            rawItem.field_0x50 = ElfStringPointer.ResolveRelocation(relas, 80, baseOffset);
            rawItem.direction_str = ElfStringPointer.ResolveRelocation(relas, 96, baseOffset);
            rawItem.enter_event_str = ElfStringPointer.ResolveRelocation(relas, 144, baseOffset);
            rawItem.exit_event_str = ElfStringPointer.ResolveRelocation(relas, 152, baseOffset);

            return rawItem;
        }
    }
}
