using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.CustomDataTypes.Registry
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct RawNpcModelState {
        public Pointer field_0x0;
        public Pointer state0_ptr;
        public int field_0x10;
        public int field_0x14;
        public Pointer field_0x18;
        public Pointer field_0x20;
        public int field_0x28;
        public int field_0x2c;
    }
}