using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.CustomDataTypes
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct NpcModelState {
        public string field_0x0;
        public string state0_ptr;
        public int field_0x10;
        public int field_0x14;
        public int field_0x18;
        public int field_0x1c;
        public int field_0x20;
        public int field_0x24;
        public int field_0x28;
        public int field_0x2c;
    }
}