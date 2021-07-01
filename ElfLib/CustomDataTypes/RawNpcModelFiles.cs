using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.CustomDataTypes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [StructLayout(LayoutKind.Sequential)]
    public struct RawNpcModelFiles {
        public ElfStringPointer model_folder;
        public ElfStringPointer model_name;
        public ElfStringPointer field_0x10;
        public ElfStringPointer field_0x18;
        public int field_0x20;
        public int field_0x24;
        public ElfStringPointer field_0x28;
        public ElfStringPointer field_0x30;
        public ElfStringPointer field_0x38;
        public int field_0x40;
        public int field_0x44;
        public int field_0x48;
        public int field_0x4c;
    }
}