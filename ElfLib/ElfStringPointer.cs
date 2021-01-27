using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib
{
    public struct ElfStringPointer
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

        internal static ElfStringPointer ResolveRelocation(List<SectionRela> relas, long offset)
        {
            SectionRela currentRela = relas.Find(x => x.Offset == offset);
            return currentRela != null ? new ElfStringPointer(currentRela.Addend) : NULL;
        }

        public static readonly ElfStringPointer NULL = new ElfStringPointer(int.MaxValue);
    }
}
