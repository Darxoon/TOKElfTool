using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;

namespace ElfLib.CustomDataTypes.Registry
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public struct RawNpcType
    {
        public ElfStringPointer id;
        public ElfStringPointer model_id;
        public int field_0x10;
        public int field_0x14;
        public ElfStringPointer texture_subclass;
        public ElfStringPointer field_0x20;
        public ElfStringPointer instance_script_str;
        public ElfStringPointer field_0x30;
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
        public ElfStringPointer field_0x68;
        public ElfStringPointer field_0x70;
        public ElfStringPointer field_0x78;
        public ElfStringPointer field_0x80;
        public ElfStringPointer field_0x88;
        public ElfStringPointer field_0x90;
        public ElfStringPointer field_0x98;
        public ElfStringPointer field_0xa0;
        public ElfStringPointer field_0xa8;
        public ElfStringPointer field_0xb0;
        public ElfStringPointer field_0xb8;
        public ElfStringPointer field_0xc0;
        public ElfStringPointer field_0xc8;
        public ElfStringPointer field_0xd0;
        public ElfStringPointer field_0xd8;
        public ElfStringPointer field_0xe0;
        public ElfStringPointer field_0xe8;
        public ElfStringPointer field_0xf0;
        public ElfStringPointer field_0xf8;
        public ElfStringPointer field_0x100;
        public ElfStringPointer field_0x108;
    }
}
