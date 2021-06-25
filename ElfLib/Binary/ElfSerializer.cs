using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ElfLib.CustomDataTypes;
using ElfLib.CustomDataTypes.Registry;

namespace ElfLib
{
    [Serializable]
    public class ElfSerializeException : Exception
    {
        public ElfSerializeException() { }

        public ElfSerializeException(string message) : base(message) { }
    }

    public class ElfSerializer<T>
    {
        private ElfSerializer()
        {

        }

        public static byte[] SerializeBinary(ElfBinary<T> binary, GameDataType dataType)
        {
            ElfSerializer<T> serializer = new ElfSerializer<T>
            {
                binary = binary,
                dataType = dataType,
            };
            return serializer.Serialize();
        }

        private ElfBinary<T> binary;
        private GameDataType dataType;

        private byte[] stringSectionData;
        private SortedDictionary<long, ElfStringPointer> stringRelocTable;

        private byte[] Serialize()
        {
            byte[] serializedData = SerializeData(binary.Data);

            // TODO: Serialize symbols

            byte[] relaData = SerializeRelaData(stringRelocTable, dataType);

            Section[] updatedSections = UpdateSections(serializedData, relaData);

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
                section.Offset = section.Type != 0 ? outputStream.Position : 0;
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

        private Section[] UpdateSections(byte[] serializedData, byte[] relaData)
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
                        newContent = BitConverter.GetBytes(dataType != GameDataType.Maplink 
                            ? binary.Data.Aggregate(0, (amount, dataSection) => amount + dataSection.Count) 
                            : 1);
                        break;
                    default:
                        break;
                }
                updatedSections.Add(section.Clone(newContent));
            }

            return updatedSections.ToArray();
        }

        private static byte[] SerializeRelaData(SortedDictionary<long, ElfStringPointer> stringRelocTable, GameDataType dataType)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            Trace.WriteLine("String Reloc table serializing:");
            Trace.Indent();
            foreach (KeyValuePair<long, ElfStringPointer> relocEntry in stringRelocTable)
            {
                if (relocEntry.Value.AsInt != int.MaxValue)
                {
                    SectionRela rela = dataType == GameDataType.Maplink && relocEntry.Key == stringRelocTable.Last().Key 
                        ? new SectionRela(relocEntry.Key, 0x800000101, relocEntry.Value.AsLong) 
                        : new SectionRela(relocEntry.Key, relocEntry.Value);
                    rela.ToBinaryWriter(writer);
                }
            }
            Trace.Unindent();

            byte[] output = stream.ToArray();

            stream.Dispose();
            writer.Dispose();

            return output;
        }

        private byte[] SerializeData(List<Element<T>>[] data)
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

        private static void ConvertToRawObjects<T>(List<Element<T>> data, GameDataType dataType, Dictionary<string, ElfStringPointer> stringDeclarationMap, 
            List<object> rawObjects, SortedDictionary<long, ElfStringPointer> stringRelocTable, ref long dataSectionPosition)
        {
            if (dataType == GameDataType.Maplink)
            {
                int nodeSize = Marshal.SizeOf(typeof(RawMaplinkNode));
                int headerSize = Marshal.SizeOf(typeof(RawMaplinkHeader));
                foreach (Element<T> element in data)
                {
                    if (element.value is MaplinkNode node)
                    {
                        rawObjects.Add(RawMaplinkNode.From(node, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        dataSectionPosition += nodeSize;
                    }

                    if (element.value is MaplinkHeader header)
                    {
                        rawObjects.Add(RawMaplinkHeader.From(header, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        stringRelocTable.Add(dataSectionPosition + typeof(RawMaplinkHeader).GetField("nodes_start_ptr").GetFieldOffset(), new ElfStringPointer(0));
                        dataSectionPosition += headerSize;
                    }
                }

                return;
            }

            int size = Marshal.SizeOf(dataType switch
            {
                GameDataType.NPC => typeof(RawNPC),
                GameDataType.Mobj => typeof(RawMobj),
                GameDataType.Aobj => typeof(RawAobj),
                GameDataType.BShape => typeof(RawBShape),
                GameDataType.Item => typeof(RawItem),
                GameDataType.DataNpc => typeof(RawNpcType),
                GameDataType.DataItem => typeof(RawItemType),
                _ => throw new ElfSerializeException("Data Type not supported yet"),
            });


            foreach (Element<T> element in data)
            {
                rawObjects.Add(dataType switch
                {
                    GameDataType.NPC =>      RawNPC.From     ((NPC)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.Mobj =>     RawMobj.From    ((Mobj)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.Aobj =>     RawAobj.From    ((Aobj)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.BShape =>   RawBShape.From  ((BShape)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.Item =>     RawItem.From    ((Item)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.DataNpc =>  RawNpcType.From ((NpcType)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.DataItem => RawItemType.From((ItemType)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    _ => throw new ElfSerializeException("Data Type not supported yet"),
                });
                dataSectionPosition += size;
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
                        allStrings.Add(npc.object_str);
                        allStrings.Add(npc.shape_str);
                        allStrings.Add(npc.field_0x38);
                        allStrings.Add(npc.field_0x40);
                        allStrings.Add(npc.field_0x50);
                        allStrings.Add(npc.field_0x60);
                        allStrings.Add(npc.field_0x70);
                        allStrings.Add(npc.field_0x108);
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
                case GameDataType.Maplink:
                    foreach (Element<T> element in data)
                    {
                        if (element.value is MaplinkNode node)
                        {
                            allStrings.Add(node.level_str);
                            allStrings.Add(node.field_0x8);
                            allStrings.Add(node.destination_str);
                            allStrings.Add(node.field_0x18);
                            allStrings.Add(StringEnumAttribute.GetIdentifier(node.shape_str));
                            allStrings.Add(node.target_str);
                            allStrings.Add(node.field_0x50);
                            allStrings.Add(node.direction_str);
                            allStrings.Add(node.enter_event_str);
                            allStrings.Add(node.exit_event_str);
                        }

                        if (element.value is MaplinkHeader header)
                        {
                            allStrings.Add(header.level_str);
                        }
                    }
                    break;
                case GameDataType.DataNpc:
                    foreach (Element<T> element in data)
                    {
                        NpcType npc = (NpcType)(object)element.value;
                        foreach (FieldInfo field in typeof(NpcType).GetFields().Where(field => field.FieldType == typeof(string)))
                        {
                            allStrings.Add((string)field.GetValue(npc));
                        }
                    }
                    break;
                case GameDataType.DataItem:
                    foreach (Element<T> element in data)
                    {
                        ItemType npc = (ItemType)(object)element.value;
                        foreach (FieldInfo field in typeof(ItemType).GetFields().Where(field => field.FieldType == typeof(string)))
                        {
                            allStrings.Add((string)field.GetValue(npc));
                        }
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
