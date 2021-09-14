using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib
{
    internal static class Util
    {
        public const string floatFormatString = "0.0#################";

        // Field Offset
        public static long GetFieldOffset(this FieldInfo fi) => GetFieldOffset(fi.FieldHandle);

        public static long GetFieldOffset(RuntimeFieldHandle h) =>
                                       Marshal.ReadInt32(h.Value + (4 + IntPtr.Size)) & 0xFFFFFF; // I have no idea what this is

        /// <summary>
        /// Reads in a block from a file and converts it to the struct
        /// type specified by the template parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T FromBinaryReader<T>(BinaryReader reader)
        {

            // Read in a byte array
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        public static void ToBinaryWriter<T>(BinaryWriter writer, T value)
        {
            int size = Marshal.SizeOf(value);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(value, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            writer.Write(arr);
        }

        public static T RawToNormalObject<T, TRaw>(TRaw raw, Section stringSection) where T : struct where TRaw : struct
        {
            object npc = new T();
            foreach (FieldInfo npcField in typeof(T).GetFields())
            {
                FieldInfo rawNpcField = typeof(TRaw).GetField(npcField.Name);

                if (npcField.FieldType == typeof(string) || (npcField.FieldType.BaseType == typeof(Enum) && StringEnumAttribute.IsStringEnum(npcField.FieldType)))
                {
                    string str = stringSection.GetString((Pointer)rawNpcField.GetValue(raw));
                    npcField.SetValue(npc, npcField.FieldType.BaseType == typeof(Enum) ? StringEnumAttribute.GetEnumValueFromString(str, npcField.FieldType) : str);
                }
                else if (npcField.FieldType.BaseType == typeof(Enum))
                {
                    npcField.SetValue(npc, (int)rawNpcField.GetValue(raw));
                }
                else if (npcField.FieldType == typeof(bool) && rawNpcField.FieldType == typeof(int))
                {
                    npcField.SetValue(npc, (int)rawNpcField.GetValue(raw) > 0);
                }
                else if (npcField.FieldType == rawNpcField.FieldType)
                {
                    npcField.SetValue(npc, rawNpcField.GetValue(raw));
                }
                else
                    throw new Exception($"Internal error: NPC field {npcField} {rawNpcField.FieldType} and RawNPC field {rawNpcField} types don't match");
            }

            return (T)npc;
        }

        public static TRaw NormalToRawObject<TRaw, TSource>(TSource source,
            Dictionary<string, SectionPointer> stringSectionTable,
            SortedDictionary<long, SectionPointer> stringRelocTable = null,
            long baseOffset = 0)

            where TRaw : struct where TSource : struct
        {
            object rawObject = new TRaw();

            foreach (FieldInfo rawField in typeof(TRaw).GetFields())
            {
                FieldInfo field = typeof(TSource).GetField(rawField.Name);
                if (field == null)
                    throw new Exception($"Didn't find field `{rawField.Name}` in type {nameof(TSource)}");

                // string
                if (rawField.FieldType == typeof(Pointer) && (field.FieldType == typeof(string) || StringEnumAttribute.IsStringEnum(field.FieldType)))
                {
                    string str = StringEnumAttribute.IsStringEnum(field.FieldType) 
                        ? StringEnumAttribute.GetIdentifier(field.GetValue(source), field.FieldType) 
                        : (string)field.GetValue(source);
                    
                    SectionPointer stringPointer = str != null ? stringSectionTable[str] : SectionPointer.NULL;
                    
                    if (stringRelocTable != null)
                        stringRelocTable.Add(rawField.GetFieldOffset() + baseOffset, stringPointer);
                    else
                        rawField.SetValue(rawObject, stringPointer);
                }
                // pointer
                else if (rawField.FieldType == typeof(Pointer) && field.FieldType == typeof(Pointer))
                {
                    Pointer pointer = (Pointer)field.GetValue(source);
                    if (stringRelocTable != null)
                        stringRelocTable.Add(rawField.GetFieldOffset() + baseOffset, pointer != Pointer.NULL 
                            ? new SectionPointer(pointer, 0x700000101)
                            : SectionPointer.NULL); 
                    else
                        rawField.SetValue(rawObject, pointer.AsLong);
                }
                else if (field.FieldType.BaseType == typeof(Enum))
                {
                    rawField.SetValue(rawObject, field.GetValue(source));
                }
                else if (field.FieldType == typeof(bool))
                {
                    rawField.SetValue(rawObject, (bool)field.GetValue(source) ? 1 : 0);
                }
                else if (field.FieldType == rawField.FieldType)
                {
                    rawField.SetValue(rawObject, field.GetValue(source));
                }
                else
                    throw new Exception($"Types `{field.FieldType}` and `{rawField.FieldType}` didn't match on field `{field.Name}`");
            }

            return (TRaw)rawObject;
        }

        internal static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> kvp,
            out TKey key,
            out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
        
        public static long CalculatePadding(long position, long alignment) =>
            (alignment - position % alignment) % alignment;
        public static long CalculatePadding(long position, int alignment) =>
            (alignment - position % alignment) % alignment;
    }
}
