using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ElfLib.Types.Disposition;
using ElfLib.Types.Disposition.Maplink;
using ElfLib.Types.Registry;

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
        private SortedDictionary<long, SectionPointer> stringRelocTable;

        private byte[] Serialize()
        {
            (byte[] serializedData, byte[] serializedRodata, byte[] relaRodata) = SerializeData(binary.Data, binary.DataOffsets);

            // TODO: Serialize symbols

            byte[] relaData = SerializeRelaData(stringRelocTable, dataType);

            Section[] updatedSections = UpdateSections(serializedData, serializedRodata, relaData, relaRodata);

            IOrderedEnumerable<Section> offsetSortedSections = (from x in updatedSections orderby x.Offset select x);

            // Set up writing
            MemoryStream outputStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputStream);

            // Write header
            Stream headerBinStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ElfLib.header.bin");
            using (BinaryReader reader = new BinaryReader(headerBinStream))
            {
                byte[] header = reader.ReadBytes((int)headerBinStream.Length);
                header[0x3C] = (byte)updatedSections.Length;
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

        private Section[] UpdateSections(byte[] serializedData, byte[] serializedRodata, byte[] relaData, byte[] relaRodata)
        {
            List<Section> updatedSections = new List<Section>();

            foreach (Section section in binary.Sections)
            {
                Trace.WriteLine(section.Name);
                byte[] newContent = section.Name switch
                {
                    ".data" => serializedData,
                    ".rodata.str1.1" => stringSectionData,
                    ".rela.data" => relaData,
                    ".rela.rodata" => relaRodata,
                    ".rodata" => serializedRodata ?? BitConverter.GetBytes(dataType != GameDataType.Maplink
                        ? binary.Data.Aggregate(0, (amount, kvp) => amount + kvp.Value.Count)
                        : 1),
                    _ => null,
                };
                
                updatedSections.Add(section.Clone(newContent));
            }

            return updatedSections.ToArray();
        }

        private static byte[] SerializeRelaData(SortedDictionary<long, SectionPointer> stringRelocTable, GameDataType dataType)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            foreach ((long originOffset, SectionPointer pointer) in stringRelocTable)
            {
                if (pointer == SectionPointer.NULL)
                    continue;

                SectionRela rela = dataType switch
                {
                    // 0xB7E00000101 for strings and 0xB7F00000101 for pointers to .rodata
                    GameDataType.DataNpcModel => new SectionRela(originOffset, pointer.Metadata - 0x600000101 + 0xB7E00000101, pointer.Pointer),
                    
                    _ => new SectionRela(originOffset, pointer.Metadata, pointer.Pointer),
                };

                rela.ToBinaryWriter(writer);
            }

            return stream.ToArray();
        }

        private (byte[] data, byte[] rodata, byte[] relaRodata) SerializeData(Dictionary<ElfType, List<Element<T>>> data, Dictionary<ElfType, List<long>> dataOffsets)
        {
            // Prepare list of all strings
            HashSet<string> allStrings = new HashSet<string>();

            foreach ((ElfType type, List<Element<T>> list) in data)
            {
                AddAllStrings(list, dataType, allStrings);
            }


            // Write list of strings
            stringSectionData = CreateStringSectionData(allStrings,
                out Dictionary<string, SectionPointer> stringDeclarationMap);

            List<object> rawObjects = new List<object>();
            stringRelocTable = new SortedDictionary<long, SectionPointer>();

            // Convert objects to raw objects and serialize them
            long dataSectionPosition = 0;
            // TODO: collect new symbol positions

            ConvertToRawObjects(data[ElfType.Main], dataType, stringDeclarationMap, rawObjects, stringRelocTable, ref dataSectionPosition);

            if (dataType == GameDataType.Maplink)
                ConvertToRawObjects(data[ElfType.MaplinkHeader], dataType, stringDeclarationMap, rawObjects, stringRelocTable, ref dataSectionPosition);
                
            
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

            if (dataType == GameDataType.DataNpcModel)
            {
                List<(Element<T> instance, long offset)> rodataObjects = new List<(Element<T>, long)>(data[ElfType.Files].Count + data[ElfType.State].Count);
                rodataObjects.AddRange(data[ElfType.Files].Zip(dataOffsets[ElfType.Files], (element, l) => (element, l)));
                rodataObjects.AddRange(data[ElfType.State].Zip(dataOffsets[ElfType.State], (element, l) => (element, l)));
                rodataObjects.Sort((x, y) => x.offset.CompareTo(y.offset));
                
                SortedDictionary<long, SectionPointer> rodataStringRelocTable = new SortedDictionary<long, SectionPointer>();

                using MemoryStream stream = new MemoryStream();
                using BinaryWriter writer = new BinaryWriter(stream);
                
                for (int i = 0; i < rodataObjects.Count; i++)
                {
                    object rawInstance = rodataObjects[i].instance.value switch
                    {
                        NpcModelFiles files => Util.NormalToRawObject<RawNpcModelFiles, NpcModelFiles>(files,
                            stringDeclarationMap, rodataStringRelocTable, stream.Position),
                        NpcModelState state => Util.NormalToRawObject<RawNpcModelState, NpcModelState>(state,
                            stringDeclarationMap, rodataStringRelocTable, stream.Position),
                    };
                    
                    Util.ToBinaryWriter(writer, rawInstance);
                }

                return (output, stream.ToArray(), SerializeRelaData(rodataStringRelocTable, dataType));
            }

            return (output, null, null);
        }

        private static void ConvertToRawObjects(List<Element<T>> data, GameDataType dataType, Dictionary<string, SectionPointer> stringDeclarationMap, 
            List<object> rawObjects, SortedDictionary<long, SectionPointer> stringRelocTable, ref long dataSectionPosition)
        {
            if (dataType == GameDataType.Maplink)
            {
                int nodeSize = Marshal.SizeOf(typeof(RawMaplinkNode));
                int headerSize = Marshal.SizeOf(typeof(RawMaplinkHeader));

                for (int j = 0; j < data.Count; j++)
                {
                    Trace.WriteLine($"{j}/{data.Count} {data[j]}");
                    
                    if (data[j].value is MaplinkNode node)
                    {
                        Trace.WriteLine($"{j}/{data.Count} raw object!!");
                        rawObjects.Add(RawMaplinkNode.From(node, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        dataSectionPosition += nodeSize;
                    }

                    if (data[j].value is MaplinkHeader header)
                    {
                        rawObjects.Add(RawMaplinkHeader.From(header, stringDeclarationMap, stringRelocTable, dataSectionPosition));
                        stringRelocTable.Add(dataSectionPosition + typeof(RawMaplinkHeader).GetField("nodes_start_ptr").GetFieldOffset(), new SectionPointer(0, 0x800000101));
                        dataSectionPosition += headerSize;
                    }
                }

                return;
            }

            int size = Marshal.SizeOf(dataType switch
            {
                GameDataType.NPC => typeof(RawNpc),
                GameDataType.Mobj => typeof(RawMobj),
                GameDataType.Aobj => typeof(RawAobj),
                GameDataType.BShape => typeof(RawBShape),
                GameDataType.Item => typeof(RawItem),
                GameDataType.DataNpc => typeof(RawNpcType),
                GameDataType.DataItem => typeof(RawItemType),
                GameDataType.DataNpcModel => typeof(RawNpcModel),
                _ => throw new ElfSerializeException("Data Type not supported yet"),
            });


            foreach (Element<T> element in data)
            {
                rawObjects.Add(dataType switch
                {
                    GameDataType.NPC      => Util.NormalToRawObject<RawNpc,Npc>((Npc)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.Mobj     => Util.NormalToRawObject<RawMobj,Mobj>((Mobj)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.Aobj     => Util.NormalToRawObject<RawAobj,Aobj>((Aobj)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.Item     => Util.NormalToRawObject<RawItem,Item>((Item)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.BShape   => Util.NormalToRawObject<RawBShape,BShape>((BShape)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.DataNpc  => Util.NormalToRawObject<RawNpcType,NpcType>((NpcType)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.DataItem => Util.NormalToRawObject<RawItemType,ItemType>((ItemType)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
                    GameDataType.DataNpcModel => Util.NormalToRawObject<RawNpcModel,NpcModel>((NpcModel)(object)element.value, stringDeclarationMap, stringRelocTable, dataSectionPosition),
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
                        Npc npc = (Npc)(object)element.value;
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
                            if (node.shape_str != TransitionType.Null)
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
                case GameDataType.DataNpcModel:
                    foreach (Element<T> element in data)
                    {
                        switch (element.value)
                        {
                            case NpcModel model:
                                allStrings.Add(model.model_id);
                                break;
                            case NpcModelFiles files:
                                allStrings.Add(files.model_folder);
                                allStrings.Add(files.model_name);
                                allStrings.Add(files.field_0x10);
                                allStrings.Add(files.field_0x18);
                                break;
                            case NpcModelState state:
                                allStrings.Add(state.description);
                                break;
                        }
                    }
                    break;
                case GameDataType.None:
                    break;
                default:
                    throw new ElfSerializeException($"Data Type {dataType} not Supported yet");
            }
        }

        private static byte[] CreateStringSectionData(HashSet<string> allStrings, out Dictionary<string, SectionPointer> stringDeclarationMap)
        {
            stringDeclarationMap = new Dictionary<string, SectionPointer>();
            MemoryStream stream = new MemoryStream
            {
                Position = 0,
                Capacity = 4096,
            };
            BinaryWriter binaryWriter = new BinaryWriter(stream);

            foreach (string str in allStrings.Where(str => str != null))
            {
                // Add entry to dict
                stringDeclarationMap.Add(str, new SectionPointer(stream.Position, 0x600000101));

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
