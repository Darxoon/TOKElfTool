using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Registry
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct NpcModelFace
    {
        public int field_0x0;
        public int field_0x4;
        public Pointer anime_arr;
        public int anime_count;
        public int field_0x14;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct RawNpcModelFace
    {
        public int field_0x0;
        public int field_0x4;
        public Pointer anime_arr;
        public int anime_count;
        public int field_0x14;
    }
}