using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Registry
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    public struct MobjType
    {
        public string id;
        public string description;
        public string model_id;
        public int field_0x18;
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
        public string script_file_name;
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
    }
    
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    public struct RawMobjType
    {
        public Pointer id;
        public Pointer description;
        public Pointer model_id;
        public int field_0x18;
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
        public Pointer script_file_name;
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
    }
}