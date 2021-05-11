using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ElfLib.CustomDataTypes;

namespace ElfLib
{
    [Serializable]
    public class ElfSerializeException : Exception
    {
        public ElfSerializeException() { }

        public ElfSerializeException(string message) : base(message) { }
    }

    public static class ElfSerializer
    {
        public static byte[] SerializeBinary<T>(ElfBinary<T> binary, GameDataType dataType, bool updateRodataCount)
        {
            // Serialize .data and .rodata.str1.1
            Trace.WriteLine("Serializing .data and .rodata.str1.1\n");
            byte[] serializedData = SerializeData(binary.Data, dataType,
                out byte[] stringSectionData,
                out SortedDictionary<long, ElfStringPointer> stringRelocTable);

            File.WriteAllBytes("tok_elf_tool_verylongdebugdumpname2.bin", serializedData);

            // TODO: Serialize symbols

            // Serialize .rela.data
            byte[] relaData = SerializeRelaData(stringRelocTable);
            File.WriteAllBytes("tok_elf_tool_verylongdebugdumpname3.bin", relaData);

            // Make new section list
            Section[] updatedSections = UpdateSections(binary, serializedData, stringSectionData, relaData, updateRodataCount);

            // Clone and order by offset
            IOrderedEnumerable<Section> offsetSortedSections = (from x in updatedSections orderby x.Offset select x);

            // Set up writing
            MemoryStream outputStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputStream);

            // Write header
            Stream headerBinStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ElfLib.header.bin");
            using (BinaryReader reader = new BinaryReader(headerBinStream))
            {
                byte[] header = reader.ReadBytes((int)headerBinStream.Length);
                writer.Write(header);
                Trace.WriteLine(Encoding.ASCII.GetString(header), header.ToString());
            }


            // Write sections
            foreach (Section section in offsetSortedSections)
            {
                bool needsPadding = section.AddrAlign > 1;
                if (needsPadding)
                {
                    long padding = Util.CalculatePadding(outputStream.Position, section.AddrAlign);
                    writer.Write(new byte[padding]);
                    Trace.WriteLine($"Padding section {section.Name,16} by {padding} amount of bytes, to pos 0x{outputStream.Position:X4} length 0x{section.Content.Length:X2}");
                }
                else
                    Trace.WriteLine($"Leaving section {section.Name,16} to pos 0x{outputStream.Position:X4} length 0x{section.Content.Length:X2}");
                section.Offset = outputStream.Position;
                writer.Write(section.Content);
            }

            // Update section header pointer
            {
                long padding = Util.CalculatePadding(outputStream.Position, 8);
                writer.Write(new byte[padding]);

                long tempPosition = outputStream.Position;
                outputStream.Position = 0x28;
                writer.Write(tempPosition);
                outputStream.Position = tempPosition;
            }

            // Write section header table
            foreach (Section section in updatedSections)
            {
                long padding = Util.CalculatePadding(outputStream.Position, 8);
                writer.Write(new byte[padding]);

                section.ToBinaryWriter(writer);
            }

            byte[] output = outputStream.ToArray();

            outputStream.Dispose();
            writer.Dispose();

            return output;
        }

        private static Section[] UpdateSections<T>(ElfBinary<T> binary, byte[] serializedData, byte[] stringSectionData, byte[] relaData, bool updateRodataCount)
        {
            List<Section> updatedSections = new List<Section>();

            foreach (Section section in binary.Sections)
            {
                Trace.WriteLine(section.Name);
                byte[] newContent = null;
                switch (section.Name)
                {
                    case ".data":
                        newContent = serializedData;
                        break;
                    case ".rodata.str1.1":
                        newContent = stringSectionData;
                        break;
                    case ".rela.data":
                        newContent = relaData;
                        break;
                    case ".rodata":
                        newContent = updateRodataCount ? BitConverter.GetBytes(binary.Data.Aggregate(0, (amount, dataSection) => amount + dataSection.Count)) : null;
                        break;
                    default:
                        break;
                }
                updatedSections.Add(section.Clone(newContent));
            }

            return updatedSections.ToArray();
        }

        private static byte[] SerializeRelaData(SortedDictionary<long, ElfStringPointer> stringRelocTable)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            Trace.WriteLine("String Reloc table serializing:");
            Trace.Indent();
            foreach (var relocEntry in stringRelocTable)
            {
                if (relocEntry.Value.AsInt != int.MaxValue)
                {
                    SectionRela rela = new SectionRela(relocEntry.Key, relocEntry.Value);
                    rela.ToBinaryWriter(writer);
                    Trace.WriteLine(rela.ToString());
                }
            }
            Trace.Unindent();

            byte[] output = stream.ToArray();

            stream.Dispose();
            writer.Dispose();

            return output;
        }

        private static byte[] SerializeData<T>(List<Element<T>>[] data, GameDataType dataType,
            out byte[] stringSectionData,
            out SortedDictionary<long, ElfStringPointer> stringRelocTable)
        {
            // Prepare list of all strings
            HashSet<string> allStrings = new HashSet<string>();

            foreach (List<Element<T>> list in data)
            {
                AddAllStrings(list, dataType, allStrings);
            }


            // Write list of strings
            stringSectionData = CreateStringSectionData(allStrings,
                out Dictionary<string, ElfStringPointer> stringDeclarationMap);

            #region Logging
            Trace.WriteLine("all strings: " + string.Join(", ", allStrings.ToArray()));
            Trace.WriteLine("all string offsets:");
            Trace.Indent();
            foreach (var item in stringDeclarationMap)
            {
                Trace.WriteLine($"String \"{item.Key}\" -> {item.Value}");
            }
            Trace.Unindent();
            File.WriteAllBytes("tok_elf_tool_verylongdebugdumpname.bin", stringSectionData);
            #endregion

            List<object> rawObjects = new List<object>();
            stringRelocTable = new SortedDictionary<long, ElfStringPointer>();

            // Convert objects to raw objects and serialize them
            long dataSectionPosition = 0;
            // TODO: collect new symbol positions

            foreach (List<Element<T>> list in data)
            {
                ConvertToRawObjects(list, dataType, stringDeclarationMap, rawObjects, stringRelocTable, ref dataSectionPosition);
            }

            #region Logging
            Trace.WriteLine("Raw NPC's:");
            Trace.Indent();
            foreach (object item in rawObjects)
            {
                Trace.WriteLine(item);
            }
            Trace.Unindent();
            Trace.WriteLine("Sting Reloc Table:");
            Trace.Indent();
            foreach (var item in stringRelocTable)
            {
                Trace.WriteLine($"{item.Key} -> {item.Value}");
            }
            Trace.Unindent();
            #endregion

            // Serialize 
            MemoryStream dataStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(dataStream);
            foreach (var item in rawObjects)
            {
                Util.ToBinaryWriter(binaryWriter, item);
            }

            byte[] output = dataStream.ToArray();

            dataStream.Dispose();
            binaryWriter.Dispose();

            return output;
        }

        private static void ConvertToRawObjects<T>(List<Element<T>> data, GameDataType dataType, Dictionary<string, ElfStringPointer> stringDeclarationMap, List<object> rawObjects, SortedDictionary<long, ElfStringPointer> stringRelocTable, ref long dataSectionPosition)
        {
            switch (dataType)
            {
                case GameDataType.NPC:
                    foreach (Element<T> element in data)
                    {
                        rawObjects.Add(RawNPC.From((NPC)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        dataSectionPosition += Marshal.SizeOf(typeof(RawNPC));
                    }
                    break;
                case GameDataType.Mobj:
                    foreach (Element<T> element in data)
                    {
                        rawObjects.Add(RawMobj.From((Mobj)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        dataSectionPosition += Marshal.SizeOf(typeof(RawMobj));
                    }
                    break;
                case GameDataType.Aobj:
                    foreach (Element<T> element in data)
                    {
                        rawObjects.Add(RawAobj.From((Aobj)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        dataSectionPosition += Marshal.SizeOf(typeof(RawAobj));
                    }
                    break;
                case GameDataType.BShape:
                    foreach (Element<T> element in data)
                    {
                        rawObjects.Add(RawBShape.From((BShape)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        dataSectionPosition += Marshal.SizeOf(typeof(RawBShape));
                    }
                    break;
                case GameDataType.Item:
                    foreach (Element<T> element in data)
                    {
                        rawObjects.Add(RawItem.From((Item)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        dataSectionPosition += Marshal.SizeOf(typeof(RawItem));
                    }
                    break;
                default:
                    throw new ElfSerializeException("Data Type not supported yet");
            }
        }

        private static void AddAllStrings<T>(List<Element<T>> data, GameDataType dataType, HashSet<string> allStrings)
        {
            switch (dataType)
            {
                case GameDataType.NPC:
                    foreach (Element<T> element in data)
                    {
                        NPC npc = (NPC)(object)element.value;
                        allStrings.Add(npc.level_str);
                        allStrings.Add(npc.obj_str);
                        allStrings.Add(npc.shape_str);
                        allStrings.Add(npc.enemy_encounter_str);
                        allStrings.Add(npc.init_function_str);
                        allStrings.Add(npc.talk_function_str);
                        allStrings.Add(npc.action_function_str);
                    }
                    break;
                case GameDataType.Mobj:
                    foreach (Element<T> element in data)
                    {
                        Mobj npc = (Mobj)(object)element.value;
                        allStrings.Add(npc.level_str);
                        allStrings.Add(npc.obj_str);
                        allStrings.Add(npc.shape_str);
                        allStrings.Add(npc.init_function_str);
                    }
                    break;
                case GameDataType.Aobj:
                    foreach (Element<T> element in data)
                    {
                        Aobj npc = (Aobj)(object)element.value;
                        allStrings.Add(npc.level_str);
                        allStrings.Add(npc.obj_str);
                        allStrings.Add(npc.shape_str);
                        //allStrings.Add(npc.init_function_str);
                    }
                    break;
                case GameDataType.BShape:
                    foreach (Element<T> element in data)
                    {
                        BShape npc = (BShape)(object)element.value;
                        allStrings.Add(npc.level_str);
                        allStrings.Add(npc.shape_str);
                        allStrings.Add(npc.field_40);
                        //allStrings.Add(npc.init_function_str);
                    }
                    break;
                case GameDataType.Item:
                    foreach (Element<T> element in data)
                    {
                        Item npc = (Item)(object)element.value;
                        allStrings.Add(npc.level_str);
                        allStrings.Add(npc.shape_str);
                        allStrings.Add(npc.obj_str);
                    }
                    break;
                case GameDataType.None:
                    break;
                default:
                    throw new ElfSerializeException($"Data Type {dataType} not Supported yet");
            }
        }

        private static byte[] CreateStringSectionData(HashSet<string> allStrings, out Dictionary<string, ElfStringPointer> stringDeclarationMap)
        {
            stringDeclarationMap = new Dictionary<string, ElfStringPointer>();
            MemoryStream stream = new MemoryStream
            {
                Position = 0,
                Capacity = 4096,
            };
            BinaryWriter binaryWriter = new BinaryWriter(stream);

            foreach (string str in allStrings.Where(str => str != null))
            {
                // Add entry to dict
                stringDeclarationMap.Add(str, new ElfStringPointer(stream.Position));

                // Create char[]
                char[] chars = new char[str.Length + 1];
                for (int i = 0; i < str.Length; i++)
                {
                    chars[i] = str[i];
                }
                chars[chars.Length - 1] = '\0';
                // Write char array
                binaryWriter.Write(chars);
            }
            byte[] output = stream.GetBuffer().Take((int)stream.Position).ToArray();

            stream.Dispose();
            binaryWriter.Dispose();

            return output;
        }
    }
}
