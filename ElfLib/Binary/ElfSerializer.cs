using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ElfLib
{
    public static class ElfSerializer
    {
        public static byte[] SerializeBinary<T>(ElfBinary<T> binary, GameDataType dataType)
        {
            // Serialize .data and .rodata.str1.1
            Trace.WriteLine("Serializing .data and .rodata.str1.1\n");
            byte[] serializedData = SerializeData(binary.Data, dataType,
                out byte[] stringSectionData,
                out var stringRelocTable);

            File.WriteAllBytes("tok_elf_tool_verylongdebugdumpname2.bin", serializedData);

            // Serialize .rela.data
            byte[] relaData = SerializeRelaData(stringRelocTable);
            File.WriteAllBytes("tok_elf_tool_verylongdebugdumpname3.bin", relaData);

            // Make new section list
            Section[] updatedSections = UpdateSections(binary, serializedData, stringSectionData, relaData);

            // Clone and order by offset
            Section[] offsetSortedSections = (from x in updatedSections orderby x.Offset select x).ToArray();

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
                    long padding = (section.AddrAlign - (outputStream.Position % section.AddrAlign)) % section.AddrAlign;
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
                long padding = (8 - (outputStream.Position % 8)) % 8;
                writer.Write(new byte[padding]);

                long tempPosition = outputStream.Position;
                outputStream.Position = 0x28;
                writer.Write(tempPosition);
                outputStream.Position = tempPosition;
            }

            // Write section header table
            foreach (Section section in updatedSections)
            {
                long padding = (8 - (outputStream.Position % 8)) % 8;
                writer.Write(new byte[padding]);

                section.ToBinaryWriter(writer);
            }

            byte[] output = outputStream.ToArray();

            writer.Dispose();
            outputStream.Dispose();

            return output;
        }

        private static Section[] UpdateSections<T>(ElfBinary<T> binary, byte[] serializedData, byte[] stringSectionData, byte[] relaData)
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

            writer.Dispose();
            stream.Dispose();

            return output;
        }

        private static byte[] SerializeData<T>(List<Element<T>> data, GameDataType dataType,
            out byte[] stringSectionData,
            out SortedDictionary<long, ElfStringPointer> stringRelocTable)
        {
            // Prepare list of all strings
            HashSet<string> allStrings = new HashSet<string>();

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
                default:
                    throw new NotImplementedException($"Data Type {dataType} not Supported yet");
            }


            // Write list of strings
            CreateStringSectionData(allStrings,
                out Dictionary<string, ElfStringPointer> stringDeclarationMap,
                out stringSectionData);

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
            switch (dataType)
            {
                case GameDataType.NPC:
                    foreach (Element<T> element in data)
                    {
                        rawObjects.Add(RawNPC.FromNPC((NPC)(object)element.value, stringDeclarationMap, stringRelocTable));
                    }
                    break;
                default:
                    throw new NotImplementedException("Data Type not supported yet");
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
                Util.ToBinaryWriter<RawNPC>(binaryWriter, (RawNPC)item);
            }

            byte[] output = dataStream.ToArray();

            binaryWriter.Dispose();
            dataStream.Dispose();

            return output;
        }

        private static void CreateStringSectionData(HashSet<string> allStrings, out Dictionary<string, ElfStringPointer> stringDeclarationMap, out byte[] sectionData)
        {
            stringDeclarationMap = new Dictionary<string, ElfStringPointer>();
            MemoryStream stream = new MemoryStream
            {
                Position = 0,
                Capacity = 4096,
            };
            BinaryWriter binaryWriter = new BinaryWriter(stream);

            foreach (string str in allStrings)
            {
                if (str != null)
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
            }
            sectionData = stream.GetBuffer().Take((int)stream.Position).ToArray();

            binaryWriter.Dispose();
            stream.Dispose();
        }
    }
}
