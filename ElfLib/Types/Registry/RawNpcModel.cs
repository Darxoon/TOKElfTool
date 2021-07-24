using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Registry
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct RawNpcModel {
        public Pointer model_id;
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
        public Pointer model_files_ptr;
        public int model_files_count;
        public int field_0x7c;
        public Pointer state_ptr;
        public int state_count;
        public int field_0x8c;
    }
}