using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.CustomDataTypes
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct NpcModelState {
        public string field_0x0;
        public long state0_ptr;
        public int field_0x10;
        public int field_0x14;
        public string field_0x18;
        public long field_0x20;
        public int field_0x28;
        public int field_0x2c;
    }
}