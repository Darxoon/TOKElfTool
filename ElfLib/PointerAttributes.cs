using System;

namespace ElfLib
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PointerAttribute : Attribute
    {
        public ElfType Location { get; }

        public PointerAttribute(ElfType location)
        {
            Location = location;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class PointerArrayLengthAttribute : Attribute
    {
        
    }
}