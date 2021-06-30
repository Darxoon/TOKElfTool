using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib
{
    public readonly struct ElfStringPointer
    {
        private readonly long pointer;

        public long AsLong => pointer;
        public int AsInt => (int)pointer;

        public ElfStringPointer(long pointer)
        {
            this.pointer = pointer;
        }
        public override string ToString()
        {
            return $"str->0x{pointer:X2}";
        }

        public static readonly ElfStringPointer NULL = new ElfStringPointer(int.MaxValue);
    }
}
