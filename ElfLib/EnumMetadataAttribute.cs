using System;
using System.Collections.Generic;
using System.Text;

namespace ElfLib
{
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumMetadataAttribute : Attribute
    {
        public string DisplayName { get; set; }

        public EnumMetadataAttribute()
        {
        }
    }
}
