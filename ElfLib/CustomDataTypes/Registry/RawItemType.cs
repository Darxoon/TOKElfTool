using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib.CustomDataTypes.Registry
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [StructLayout(LayoutKind.Sequential)]
    public struct RawItemType
    {
        public Pointer id;
        public Pointer name;
        public Pointer type;
        public Pointer model_internal_id;
        public int field_0x20;
        public int field_0x24;
        public int field_0x28;
        public int field_0x2c;
        public int field_0x30;
        public int buy_price;
        public int sell_price;
        public int field_0x3c;
        public int value;
        public int field_0x44;
        public Pointer model_base_path;
        public Pointer field_0x50;
        public Pointer description_id;
        public Pointer icon_id;
        public int field_0x68;
        public int field_0x6c;
        public Pointer field_0x70;
        public Pointer field_0x78;
        public int field_0x80;
        public int field_0x84;
        public Pointer field_0x88;
        public Pointer field_0x90;
        public Pointer field_0x98;
        public Pointer field_0xa0;
        public int field_0xa8;
        public int field_0xac;
        public int field_0xb0;
        public int field_0xb4;
        public int field_0xb8;
        public int field_0xbc;
        public int field_0xc0;
        public int field_0xc4;
        public Pointer script_id;
    }
}
