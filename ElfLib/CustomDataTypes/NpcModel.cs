using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.CustomDataTypes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [StructLayout(LayoutKind.Sequential)]
    public struct NpcModel {
        public string model_id;
        public Vector3 field_0x8;
        public short field_0x14;
        public short field_0x16;
        public short field_0x18;
        public short field_0x1a;
        public int field_0x1c;
        public int field_0x20;
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
        public float field_0x54;
        public float field_0x58;
        public float field_0x5c;
        public float field_0x60;
        public float field_0x64;
        public int field_0x68;
        public int field_0x6c;
        public long model_files_ptr;
        public int field_0x78;
        public int field_0x7c;
        public long state_ptr;
        public int field_0x88;
        public int field_0x8c;
    }
}