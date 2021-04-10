using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    public struct RawItem
    {
        public ElfStringPointer level_str;
        public ElfStringPointer obj_str;
        public ElfStringPointer shape_str;
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

        public static RawItem From(Item item, Dictionary<string, ElfStringPointer> stringSectionTable, SortedDictionary<long, ElfStringPointer> stringRelocTable = null, long baseOffset = 0)
            => Util.NormalToRawObject<RawItem, Item>(item, stringSectionTable, stringRelocTable, baseOffset);

        internal static RawItem ReadBinaryData(BinaryReader binaryReader, List<SectionRela> relas, long baseOffset)
        {
            RawItem rawItem = Util.FromBinaryReader<RawItem>(binaryReader);
            rawItem.level_str = ElfStringPointer.ResolveRelocation(relas, 0, baseOffset);
            rawItem.obj_str = ElfStringPointer.ResolveRelocation(relas, 8, baseOffset);
            rawItem.shape_str = ElfStringPointer.ResolveRelocation(relas, 16, baseOffset);

            return rawItem;
        }
    }
}
