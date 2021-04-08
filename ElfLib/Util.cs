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

            Trace.WriteLine($"ToBinaryWriter {typeof(T).Name} {arr.Length:X2}");

            writer.Write(arr);
        }

        public static T RawToNormalObject<T, TRaw>(TRaw raw, Section stringSection) where T : struct where TRaw : struct
        {
            object npc = new T();
            Trace.WriteLine("Loading Mobj from RawMobj");
            Trace.Indent();
            foreach (FieldInfo npcField in typeof(T).GetFields())
            {
                FieldInfo rawNpcField = typeof(TRaw).GetField(npcField.Name);
                //Trace.WriteLine($"NPC: {npcField.Name}: {npcField.FieldType.Name}, \tRawNPC: {rawNpcField.Name}: {rawNpcField.FieldType.Name}");
                if (npcField.FieldType == typeof(string))
                {
                    Trace.WriteLine("Peter bghjmnv ");
                    Trace.WriteLine(rawNpcField.Name);
                    Trace.WriteLine(rawNpcField.GetValue(raw));
                    string str = stringSection.GetString((ElfStringPointer)rawNpcField.GetValue(raw));
                    npcField.SetValue(npc, str);
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
            Trace.Unindent();

            return (T)npc;
        }

        public static TRaw NormalToRawObject<TRaw, TSource>(TSource npc,
            Dictionary<string, ElfStringPointer> stringSectionTable,
            SortedDictionary<long, ElfStringPointer> stringRelocTable = null,
            long baseOffset = 0)

            where TRaw : struct where TSource : struct
        {
            object rawNPC = new TRaw();

            foreach (FieldInfo rawNpcField in typeof(RawNPC).GetFields())
            {
                FieldInfo npcField = typeof(NPC).GetField(rawNpcField.Name);
                if (npcField == null)
                    throw new Exception($"Didn't find field `{rawNpcField.Name}` in type NPC");

                if (rawNpcField.FieldType == typeof(ElfStringPointer) && npcField.FieldType == typeof(string))
                {
                    string str = (string)npcField.GetValue(npc);
                    ElfStringPointer stringPointer = str != null ? stringSectionTable[str] : ElfStringPointer.NULL;
                    if (stringRelocTable != null)
                        stringRelocTable.Add(rawNpcField.GetFieldOffset() + baseOffset, stringPointer);
                    else
                        rawNpcField.SetValue(rawNPC, stringPointer);
                }
                else if (npcField.FieldType.BaseType == typeof(Enum))
                {
                    rawNpcField.SetValue(rawNPC, npcField.GetValue(npc));
                }
                else if (npcField.FieldType == typeof(bool))
                {
                    rawNpcField.SetValue(rawNPC, (bool)npcField.GetValue(npc) ? 1 : 0);
                }
                else if (npcField.FieldType == rawNpcField.FieldType)
                {
                    rawNpcField.SetValue(rawNPC, npcField.GetValue(npc));
                }
                else
                    throw new Exception($"Types `{npcField.FieldType}` and `{rawNpcField.FieldType}` didn't match on field `{npcField.Name}`");
            }

            return (TRaw)rawNPC;
        }

        public static long CalculatePadding(long position, long alignment) =>
            (alignment - position % alignment) % alignment;
        public static long CalculatePadding(long position, int alignment) =>
            (alignment - position % alignment) % alignment;
    }
}
