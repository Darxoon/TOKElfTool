﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    public struct RawAobj
    {
        public ElfStringPointer level_str;
        public ElfStringPointer obj_str;
        public ElfStringPointer shape_str;
        public Vector3 position;
        public Vector3 rotation;
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
        public int field_0x74;
        public int field_0x78;
        public int field_0x7c;
        public int field_0x80;
        public int field_0x84;
        public int field_0x88;
        public int field_0x8c;
        public int field_0x90;
        public int field_0x94;
        public int field_0x98;
        public int field_0x9c;
        public int field_0xa0;
        public int field_0xa4;
        public int field_0xa8;
        public float field_0xac;
        public int field_0xb0;
        public int field_0xb4;
        public int field_0xb8;
        public int field_0xbc;
        public int field_0xc0;
        public int field_0xc4;
        public int field_0xc8;
        public int field_0xcc;
        public int field_0xd0;
        public int field_0xd4;
        public int field_0xd8;
        public int field_0xdc;
        public int field_0xe0;
        public int field_0xe4;
        public int field_0xe8;
        public int field_0xec;
        public int field_0xf0;
        public int field_0xf4;
        public int field_0xf8;
        public int field_0xfc;
        public int field_0x100;
        public int field_0x104;
        public int field_0x108;
        public int field_0x10c;
        public int field_0x110;
        public int field_0x114;
        public int field_0x118;
        public int field_0x11c;
        public int field_0x120;
        public int field_0x124;
        public int field_0x128;
        public int field_0x12c;
        public int field_0x130;
        public int field_0x134;
        public int field_0x138;
        public int field_0x13c;
        public int field_0x140;
        public int field_0x144;
        public int field_0x148;
        public int field_0x14c;
        public int field_0x150;
        public int field_0x154;
        public int field_0x158;
        public int field_0x15c;
        public int field_0x160;
        public int field_0x164;
        public int field_0x168;
        public int field_0x16c;
        public int field_0x170;
        public int field_0x174;

        public static RawAobj From(Aobj npc, Dictionary<string, ElfStringPointer> stringSectionTable, SortedDictionary<long, ElfStringPointer> stringRelocTable = null, long baseOffset = 0) 
            => Util.NormalToRawObject<RawAobj, Aobj>(npc, stringSectionTable, stringRelocTable, baseOffset);

        internal static RawAobj ReadBinaryData(BinaryReader binaryReader, List<SectionRela> relas, long baseOffset)
        {
            RawAobj rawMobj = Util.FromBinaryReader<RawAobj>(binaryReader);
            rawMobj.level_str = ElfStringPointer.ResolveRelocation(relas, 0, baseOffset);
            rawMobj.obj_str = ElfStringPointer.ResolveRelocation(relas, 8, baseOffset);
            rawMobj.shape_str = ElfStringPointer.ResolveRelocation(relas, 16, baseOffset);


            return rawMobj;
        }
    }
}
