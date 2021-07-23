using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib
{
    public readonly struct Pointer
    {
        private readonly long pointer;

        public long AsLong => pointer;
        public int AsInt => (int)pointer;

        public Pointer(long pointer)
        {
            this.pointer = pointer;
        }
        public override string ToString()
        {
            return $"ptr->0x{pointer:X2}";
        }

        #region things that should be implemented by default because this is a struct and not a class, if I needed something different than memberwise comparison I would use a class
        public static bool operator ==(Pointer x, Pointer y)
        {
            return x.pointer == y.pointer;
        }

        public static bool operator !=(Pointer x, Pointer y)
        {
            return !(x == y);
        }

        public override bool Equals(object obj)
        {
            return obj is Pointer other && this == other;
        }
        
        public bool Equals(Pointer other)
        {
            return pointer == other.pointer;
        }
        
        public override int GetHashCode()
        {
            return pointer.GetHashCode();
        }
        #endregion

        public static readonly Pointer NULL = new Pointer(int.MaxValue);
        public static readonly Pointer ZERO = new Pointer(0);
    }
    
    
    internal readonly struct SectionPointer
    {
        private const long DEFAULT_METADATA = long.MaxValue - 2;
        public static readonly SectionPointer NULL = new SectionPointer(ElfLib.Pointer.NULL, DEFAULT_METADATA);
        
        
        private readonly Pointer pointer;

        public Pointer Pointer => pointer;
        public long AsLong => pointer.AsLong;
        public int AsInt => pointer.AsInt;
        public long Metadata { get; }

        public SectionPointer(Pointer pointer, long sectionMetadata)
        {
            this.pointer = pointer;
            Metadata = sectionMetadata;
        }
        
        public SectionPointer(Pointer pointer)
        {
            this.pointer = pointer;
            Metadata = DEFAULT_METADATA;
        }
        
        public SectionPointer(long pointer, long sectionMetadata)
        {
            this.pointer = new Pointer(pointer);
            Metadata = sectionMetadata;
        }
        
        public SectionPointer(long pointer)
        {
            this.pointer = new Pointer(pointer);
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
