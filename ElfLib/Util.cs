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

            Trace.WriteLine("ToBinaryWriter " + typeof(T).Name + " " + arr.Length.ToString("X2"));

            writer.Write(arr);
        }

        public static long CalculatePadding(long position, long alignment) =>
            (alignment - position % alignment) % alignment;
        public static long CalculatePadding(long position, int alignment) =>
            (alignment - position % alignment) % alignment;
    }
}
