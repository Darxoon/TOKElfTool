using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public struct RawMaplinkHeader
    {
        public ElfStringPointer level_str;
        public int last_element_index;
        public int field_0xc;
        public long nodes_start_ptr;
        public int field_0x18;
        public int field_0x1c;
        public int field_0x20;
        public int field_0x24;
        public int field_0x28;
        public int field_0x2c;

        public static RawMaplinkHeader From(MaplinkHeader npc, Dictionary<string, ElfStringPointer> stringSectionTable, SortedDictionary<long, ElfStringPointer> stringRelocTable = null, long baseOffset = 0)
            => Util.NormalToRawObject<RawMaplinkHeader, MaplinkHeader>(npc, stringSectionTable, stringRelocTable, baseOffset);
    };
}
