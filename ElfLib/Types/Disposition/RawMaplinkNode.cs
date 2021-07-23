using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Disposition
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [StructLayout(LayoutKind.Sequential)]
    public struct RawMaplinkNode
    {
        public Pointer level_str;
        public Pointer field_0x8;
        public Pointer destination_str;
        public Pointer field_0x18;
        public Pointer shape_str;
        public Pointer target_str;
        public float field_0x30;
        public int field_0x34;
        public int field_0x38;
        public int field_0x3c;
        public int field_0x40;
        public int field_0x44;
        public int field_0x48;
        public int field_0x4c;
        public Pointer field_0x50;
        public int field_0x58;
        public int field_0x5c;
        public Pointer direction_str;
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
        public Pointer enter_event_str;
        public Pointer exit_event_str;
        public int field_0xa0;
        public int field_0xa4;
        public int field_0xa8;
        public int field_0xac;

        internal static RawMaplinkNode From(MaplinkNode item, Dictionary<string, SectionPointer> stringSectionTable, SortedDictionary<long, SectionPointer> stringRelocTable = null, long baseOffset = 0)
            => Util.NormalToRawObject<RawMaplinkNode, MaplinkNode>(item, stringSectionTable, stringRelocTable, baseOffset);
    }
}
