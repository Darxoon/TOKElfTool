using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.CustomDataTypes
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [StructLayout(LayoutKind.Sequential)]
    public struct NpcModelFiles {
        public string model_folder;
        public string model_name;
        public string field_0x10;
        public string field_0x18;
        public int field_0x20;
        public int field_0x24;
        public string field_0x28;
        public string field_0x30;
        public string field_0x38;
        public int field_0x40;
        public int field_0x44;
        public int field_0x48;
        public int field_0x4c;
    }
}