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

        #region things that should be implemented by default because this is a struct and not a class, if I needed something different than memberwise comparison I would use a class
        public static bool operator ==(ElfStringPointer x, ElfStringPointer y)
        {
            return x.pointer == y.pointer;
        }

        public static bool operator !=(ElfStringPointer x, ElfStringPointer y)
        {
            return !(x == y);
        }

        public override bool Equals(object obj)
        {
            return obj is ElfStringPointer other && this == other;
        }
        
        public bool Equals(ElfStringPointer other)
        {
            return pointer == other.pointer;
        }
        
        public override int GetHashCode()
        {
            return pointer.GetHashCode();
        }
        #endregion

        public static readonly ElfStringPointer NULL = new ElfStringPointer(int.MaxValue);
        public static readonly ElfStringPointer ZERO = new ElfStringPointer(0);
    }
    
    public readonly struct SectionPointer
    {
        private const long DEFAULT_METADATA = long.MaxValue - 2;
        public static readonly SectionPointer NULL = new SectionPointer(ElfStringPointer.NULL, DEFAULT_METADATA);
        
        
        private readonly ElfStringPointer pointer;

        public ElfStringPointer Pointer => pointer;
        public long AsLong => pointer.AsLong;
        public int AsInt => pointer.AsInt;
        public long Metadata { get; }

        public SectionPointer(ElfStringPointer pointer, long sectionMetadata)
        {
            this.pointer = pointer;
            Metadata = sectionMetadata;
        }
        
        public SectionPointer(ElfStringPointer pointer)
        {
            this.pointer = pointer;
            Metadata = DEFAULT_METADATA;
        }
        
        public SectionPointer(long pointer, long sectionMetadata)
        {
            this.pointer = new ElfStringPointer(pointer);
            Metadata = sectionMetadata;
        }
        
        public SectionPointer(long pointer)
        {
            this.pointer = new ElfStringPointer(pointer);
            Metadata = DEFAULT_METADATA;
        }
        
        public override string ToString()
        {
            return $"+{pointer}:{(Metadata == DEFAULT_METADATA ? " undefined" : $"0x{Metadata:X}")}";
        }
        
        #region things that should be implemented by default because this is a struct and not a class, if I needed something different than memberwise comparison I would use a class
        public static bool operator ==(SectionPointer x, SectionPointer y)
        {
            return x.pointer == y.pointer && x.Metadata == y.Metadata;
        }

        public static bool operator !=(SectionPointer x, SectionPointer y)
        {
            return !(x == y);
        }

        public override bool Equals(object obj)
        {
            return obj is SectionPointer other && this == other;
        }
        
        public bool Equals(SectionPointer other)
        {
            return this == other;
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                hash = 13 * hash + pointer.GetHashCode();
                hash = 13 * hash + Metadata.GetHashCode();
                return hash;
            }
        }
        #endregion
    }
}
