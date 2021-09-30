using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Registry
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct NpcModelState {
        public string description;
        [Pointer(ElfType.SubStates)] 
        public Pointer substate_arr;
        [PointerArrayLength] 
        public int substate_count;
        public int field_0x14;
    }
}