using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Registry
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [StructLayout(LayoutKind.Sequential)]
    public struct RawNpcModelFiles {
        public Pointer model_folder;
        public Pointer model_name;
        public Pointer field_0x10;
        public Pointer field_0x18;
        public int field_0x20;
        public int field_0x24;
    }
}