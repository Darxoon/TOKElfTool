using System;
using System.Collections.Generic;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    public struct Item
    {
        public string level_str;
        public string obj_str;
        public string shape_str;
        public Vector3 field_0x18;
        public int field_0x24;
        public int field_0x28;
        public int field_0x2c;
        public int field_0x30;
        public int field_0x34;
        public int field_0x38;
        public int field_0x3c;
        public int field_0x40;
        public int field_0x44;
        public int field_0x48;
        public int field_0x4c;
        public int field_0x50;
        public int field_0x54;
        public int field_0x58;
        public int field_0x5c;
        public int field_0x60;
        public int field_0x64;
        public int field_0x68;
        public int field_0x6c;
        public int field_0x70;
        public float field_0x74;
        public int field_0x78;
        public int field_0x7c;

        public static Item From(RawItem rawItem, Section stringSection) => Util.RawToNormalObject<Item, RawItem>(rawItem, stringSection);
    }
}
