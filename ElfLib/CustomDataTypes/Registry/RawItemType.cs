﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib.CustomDataTypes.Registry
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawItemType
    {
        public ElfStringPointer id;
        public ElfStringPointer field_0x8;
        public ElfStringPointer field_0x10;
        public ElfStringPointer field_0x18;
        public int field_0x20;
        public int field_0x24;
        public int field_0x28;
        public int field_0x2c;
        public int field_0x30;
        public int field_0x34;
        public int field_0x38;
        public int field_0x3c;
        public int value;
        public int field_0x44;
        public ElfStringPointer model_id;
        public ElfStringPointer field_0x50;
        public ElfStringPointer field_0x58;
        public ElfStringPointer field_0x60;
        public int field_0x68;
        public int field_0x6c;
        public ElfStringPointer field_0x70;
        public ElfStringPointer field_0x78;
        public int field_0x80;
        public int field_0x84;
        public ElfStringPointer field_0x88;
        public ElfStringPointer field_0x90;
        public ElfStringPointer field_0x98;
        public ElfStringPointer field_0xa0;
        public int field_0xa8;
        public int field_0xac;
        public int field_0xb0;
        public int field_0xb4;
        public int field_0xb8;
        public int field_0xbc;
        public int field_0xc0;
        public int field_0xc4;
        public ElfStringPointer field_0xc8;

        public static RawItemType From(ItemType itemType, Dictionary<string, ElfStringPointer> stringSectionTable, SortedDictionary<long, ElfStringPointer> stringRelocTable = null, long baseOffset = 0)
            => Util.NormalToRawObject<RawItemType, ItemType>(itemType, stringSectionTable, stringRelocTable, baseOffset);

        internal static RawItemType ReadBinaryData(BinaryReader binaryReader, List<SectionRela> relas, long baseOffset)
        {
            object instance = Util.FromBinaryReader<RawItemType>(binaryReader);

            return (RawItemType)Util.ResolveStringRelocations(instance, typeof(RawItemType), relas, baseOffset);
        }
    }
}