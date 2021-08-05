using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Registry
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct NpcModelAnime
    {
        public string description;
        public string id;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct RawNpcModelAnime
    {
        public Pointer description;
        public Pointer id;
    }
}