using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ElfLib
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class StringEnumAttribute : Attribute
    {
        public bool Strict { get; set; } = true;

        public static bool IsStringEnum(Type type) => type.GetCustomAttribute(typeof(StringEnumAttribute)) != null;
        public static bool IsStringEnum<T>() where T : Enum => IsStringEnum(typeof(T));

        public static EnumMetadataAttribute GetMetadata<T>(T stringEnum)
            where T : Enum
        {
            Type type = typeof(T);
            if (type.GetCustomAttribute(typeof(StringEnumAttribute)) == null)
                return null;
            FieldInfo field = type.GetFields().Where(x => x.IsStatic).ToArray()[Array.IndexOf(Enum.GetValues(type), stringEnum)];
            EnumMetadataAttribute metadata = (EnumMetadataAttribute)field.GetCustomAttribute(typeof(EnumMetadataAttribute));
            return metadata;
        }
        public static EnumMetadataAttribute GetMetadata(object stringEnum, Type type)
        {
            if (type.GetCustomAttribute(typeof(StringEnumAttribute)) == null)
                return null;
            int index = Array.IndexOf(Enum.GetValues(type), stringEnum);
            var fields = type.GetFields().Where(x => x.IsStatic).ToArray();
            FieldInfo field = fields[index];
            EnumMetadataAttribute metadata = (EnumMetadataAttribute)field.GetCustomAttribute(typeof(EnumMetadataAttribute));
            return metadata;
        }
        public static string GetIdentifier<T>(T stringEnum) where T : Enum => GetMetadata(stringEnum).Identifier;
        public static string GetIdentifier(object stringEnum, Type type) => GetMetadata(stringEnum, type).Identifier;
        public static string GetDisplayName<T>(T stringEnum) where T : Enum => GetMetadata(stringEnum).DisplayName;
        public static string GetDisplayName(object stringEnum, Type type) => GetMetadata(stringEnum, type).DisplayName;

        public static T? GetValueFromString<T>(string str)
            where T : struct, Enum
        {
            foreach (FieldInfo fieldInfo in typeof(T).GetFields())
            {
                if(!fieldInfo.IsStatic)
                    continue;
                EnumMetadataAttribute metadata = fieldInfo.GetCustomAttribute(typeof(EnumMetadataAttribute)) as EnumMetadataAttribute;
                if (metadata == null)
                    continue;
                if (metadata.Identifier == str)
                    return (T?)fieldInfo.GetValue(null);
            }

            return null;
        }
        public static object GetEnumValueFromString(string str, Type type)
        {
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                if(!fieldInfo.IsStatic)
                    continue;
                EnumMetadataAttribute metadata = fieldInfo.GetCustomAttribute(typeof(EnumMetadataAttribute)) as EnumMetadataAttribute;
                if (metadata == null)
                    continue;
                if (metadata.Identifier == str)
                    return fieldInfo.GetValue(null);
            }

            return null;
        }
    }
}
