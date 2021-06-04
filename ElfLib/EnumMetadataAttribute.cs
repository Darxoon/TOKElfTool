using System;
using System.Collections.Generic;
using System.Text;

namespace ElfLib
{
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumMetadataAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string Identifier { get; set; } = null;

        public EnumMetadataAttribute()
        {
        }
    }
}
