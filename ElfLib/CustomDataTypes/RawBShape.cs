using System;
using System.Collections.Generic;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    public struct RawBShape
    {
        public ElfStringPointer level_str;
        public ElfStringPointer shape_str;
        public Vector3 position;
        public Vector3 rotation;
        public int field_28;
        public Vector3 unk_2C;
        public float field_38;
        public int field_3C;
        public ElfStringPointer field_40;
        public int field_48;
        public int field_4C;
    }

}
