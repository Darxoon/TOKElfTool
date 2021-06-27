using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ElfLib.CustomDataTypes.Registry
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public struct NpcType
    {
        public string id;
        public string model_id;
        public int field_0x10;
        public int field_0x14;
        public string texture_subclass;
        public string field_0x20;
        public string instance_script_str;
        public string field_0x30;
        public int field_0x38;
        public float field_0x3c;
        public float field_0x40;
        public float field_0x44;
        public float field_0x48;
        public float field_0x4c;
        public float field_0x50;
        public int field_0x54;
        public int field_0x58;
        public int field_0x5c;
        public int field_0x60;
        public int field_0x64;
        public string field_0x68;
        public string field_0x70;
        public string field_0x78;
        public string field_0x80;
        public string field_0x88;
        public string field_0x90;
        public string field_0x98;
        public string field_0xa0;
        public string field_0xa8;
        public string field_0xb0;
        public string field_0xb8;
        public string field_0xc0;
        public string field_0xc8;
        public string field_0xd0;
        public string field_0xd8;
        public string field_0xe0;
        public string field_0xe8;
        public string field_0xf0;
        public string field_0xf8;
        public string field_0x100;
        public string field_0x108;

        public static NpcType From(RawNpcType npcType, Section stringSection) => Util.RawToNormalObject<NpcType, RawNpcType>(npcType, stringSection);
    }
}
