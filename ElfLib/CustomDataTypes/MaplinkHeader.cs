using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public struct MaplinkHeader
    {
        public string level_str;
        public int last_element_index;
        public int field_0xc;
        public long nodes_start_ptr;
        public int field_0x18;
        public int field_0x1c;
        public int field_0x20;
        public int field_0x24;
        public int field_0x28;
        public int field_0x2c;

        public static MaplinkHeader From(RawMaplinkHeader rawItem, Section stringSection) => Util.RawToNormalObject<MaplinkHeader, RawMaplinkHeader>(rawItem, stringSection);
    };
}
