using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Registry
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct NpcModelSubState
    {
        public int field_0x0;
        public int field_0x4;
        [Pointer(ElfType.Face)]
        public Pointer face_arr;
        [PointerArrayLength]
        public int face_count;
        public int field_0x14;
    }
}