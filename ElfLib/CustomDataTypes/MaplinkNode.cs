using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public struct MaplinkNode
    {
        public string field_0x0;
        public string field_0x8;
        public string field_0x10;
        public string field_0x18;
        public string field_0x20;
        public string field_0x28;
        public float field_0x30;
        public int field_0x34;
        public int field_0x38;
        public int field_0x3c;
        public int field_0x40;
        public int field_0x44;
        public int field_0x48;
        public int field_0x4c;
        public string field_0x50;
        public int field_0x58;
        public int field_0x5c;
        public string field_0x60;
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
        public string field_0x90;
        public string field_0x98;
        public int field_0xa0;
        public int field_0xa4;
        public int field_0xa8;
        public int field_0xac;

        public static MaplinkNode From(RawMaplinkNode rawItem, Section stringSection) => Util.RawToNormalObject<MaplinkNode, RawMaplinkNode>(rawItem, stringSection);
    }
}
