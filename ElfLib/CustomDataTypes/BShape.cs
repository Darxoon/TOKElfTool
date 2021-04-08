using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib.CustomDataTypes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BShape
    {
        public string level_str;
        public string shape_str;
        public Vector3 position;
        public Vector3 rotation;
        public int field_28;
        public Vector3 unk_2C;
        public float field_38;
        public int field_3C;
        public string field_40;
        public int field_48;
        public int field_4C;

        public static BShape From(RawBShape rawBShape, Section stringSection) => Util.RawToNormalObject<BShape, RawBShape>(rawBShape, stringSection);
    }
}
