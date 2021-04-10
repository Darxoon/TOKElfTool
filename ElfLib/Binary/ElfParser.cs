using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using ElfLib.CustomDataTypes;

namespace ElfLib
{
    [Serializable]
    public class ElfParseException : Exception
    {
        public ElfParseException() { }

        public ElfParseException(string message) : base(message) { }
    }

    [Serializable]
    public class ElfContentNotFoundException : Exception
    {
        public ElfContentNotFoundException() { }

        public ElfContentNotFoundException(string message) : base(message) { }
    }

    public static class ElfParser
    {
        const int HEADER_LENGTH = 0x40;

        public static ElfBinary<T> ParseFile<T>(string filepath, GameDataType dataType, bool verbose = true)
        {
            FileStream input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, (int)new FileInfo(filepath).Length);
            BinaryReader reader = new BinaryReader(input);

            return ParseFile<T>(reader, dataType, verbose);

        }

        public static ElfBinary<T> ParseFile<T>(BinaryReader reader, GameDataType dataType, bool verbose = true)
        {
            Stream input = reader.BaseStream;

            // get constants from header
            input.Position = 0x28;
            int sectionHeaderTableOffset = (int)reader.ReadInt64();
            if (verbose)
                Trace.WriteLine(sectionHeaderTableOffset);
            input.Position = 60;
            int sectionAmount = reader.ReadInt16();
            input.Position = 62;
            int stringTableIndex = reader.ReadInt16();


            input.Position = sectionHeaderTableOffset;
            List<Section> sections = ParseSectionHeaderTable(reader, sectionAmount);

            // Add section content
            for (int i = 0; i < sections.Count; i++)
            {
                input.Position = sections[i].Offset;
                sections[i].Content = reader.ReadBytes((int)sections[i].Size);
                if (verbose)
                    Trace.WriteLine(sections[i].ToString());
            }

            Section stringTable = sections[stringTableIndex];

            // Add section names
            foreach (Section section in sections)
            {
                section.Name = stringTable.GetString(section.namePointer.AsInt);
            }

            if (GetSection(sections, ".data") == null)
            {
                throw new ElfContentNotFoundException("Could not find content");
            }

            List<SectionRela> relas = ParseRelocations(sections);

            if (verbose)
            {
                #region Logging
                Trace.Indent();
                foreach (Section section in sections)
                {
                    Trace.WriteLine(section);
                }
                Trace.Unindent();
                #endregion
            }
            List<T> data = ParseData(sections, relas, dataType).Cast<T>().ToList();



            if (verbose)
            {
                #region Log fields
                Trace.WriteLine("Data:");
                Trace.Indent();
                foreach (var item in data)
                {
                    Trace.WriteLine(item);
                }
                Trace.Unindent();
                #endregion
            }


            reader.Dispose();
            input.Close();

            return new ElfBinary<T>(sections, data.Select(value => new Element<T>(value)).ToList());
        }


        private static List<Section> ParseSectionHeaderTable(BinaryReader reader, int sectionAmount)
        {
            List<Section> sections = new List<Section>();

            for (int i = 0; i < sectionAmount; i++)
            {
                sections.Add(new Section(reader));
            }

            return sections;
        }

        private static Section GetSection(List<Section> sections, string name) =>
            sections.Find(section => section.Name == name);

        private static List<SectionRela> ParseRelocations(List<Section> sections)
        {
            Section relaSection = GetSection(sections, ".rela.data");
            List<SectionRela> relas = new List<SectionRela>();

            using (BinaryReader reader = relaSection.CreateBinaryReader())
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    relas.Add(SectionRela.FromBinary(reader));
                }
            }

            return relas;
        }

        private static List<object> ParseData(List<Section> sections, List<SectionRela> relas, GameDataType dataType)
        {
            Section dataSection = GetSection(sections, ".data");
            Section stringSection = GetSection(sections, ".rodata.str1.1");

            List<object> objects = new List<object>();

            byte[] data = dataSection.Content;
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);

            switch (dataType)
            {
                case GameDataType.None:
                    return null;
                case GameDataType.NPC:
                    {
                        List<RawNPC> rawNPCs = ParseData(sections, relas, GameDataType.RawNPC).Cast<RawNPC>().ToList();
                        List<NPC> npcs = rawNPCs.Select(rawNPC => NPC.From(rawNPC, stringSection)).ToList();
                        List<object> output = npcs.Cast<object>().ToList();
                        return output;
                    }
                case GameDataType.RawNPC:
                    while (stream.Position != stream.Length)
                    {
                        objects.Add(RawNPC.ReadBinaryData(reader, relas, reader.BaseStream.Position));
                    }
                    break;
                case GameDataType.Mobj:
                    {
                        IEnumerable<RawMobj> rawMobjs = ParseData(sections, relas, GameDataType.RawMobj).Cast<RawMobj>();
                        IEnumerable<Mobj> mobjs = rawMobjs.Select(rawMobj => Mobj.From(rawMobj, stringSection));
                        List<object> output = mobjs.Cast<object>().ToList();
                        return output;
                    }
                case GameDataType.RawMobj:
                    while (stream.Position != stream.Length)
                    {
                        objects.Add(RawMobj.ReadBinaryData(reader, relas, reader.BaseStream.Position));
                    }
                    break;
                case GameDataType.Aobj:
                    {
                        IEnumerable<RawAobj> rawMobjs = ParseData(sections, relas, GameDataType.RawAobj).Cast<RawAobj>();
                        IEnumerable<Aobj> mobjs = rawMobjs.Select(rawMobj => Aobj.From(rawMobj, stringSection));
                        List<object> output = mobjs.Cast<object>().ToList();
                        return output;
                    }
                case GameDataType.RawAobj:
                    while (stream.Position != stream.Length)
                    {
                        objects.Add(RawAobj.ReadBinaryData(reader, relas, reader.BaseStream.Position));
                    }
                    break;
                case GameDataType.BShape:
                    {
                        IEnumerable<RawBShape> rawMobjs = ParseData(sections, relas, GameDataType.RawBShape).Cast<RawBShape>();
                        IEnumerable<BShape> mobjs = rawMobjs.Select(rawMobj => BShape.From(rawMobj, stringSection));
                        List<object> output = mobjs.Cast<object>().ToList();
                        return output;
                    }
                case GameDataType.RawBShape:
                    while (stream.Position != stream.Length)
                    {
                        objects.Add(RawBShape.ReadBinaryData(reader, relas, reader.BaseStream.Position));
                    }
                    break;

                case GameDataType.Item:
                    return ParseObjectsOfType<Item, RawItem>(sections, relas, stringSection, GameDataType.RawItem, Item.From);
                case GameDataType.RawItem:
                    ParseRawObjectsOfType(stream, objects, reader, relas, RawItem.ReadBinaryData);
                    break;
                default:
                    throw new ElfParseException("Data type not implemented");
            }

            return objects;
        }

        private delegate T ObjectConverter<out T, in TRaw>(TRaw rawBShape, Section stringSection);

        private static List<object> ParseObjectsOfType<T, TRaw>(List<Section> sections, List<SectionRela> relas, Section stringSection,
            GameDataType rawType, ObjectConverter<T, TRaw> converter)
        {
            List<object> rawObjects = ParseData(sections, relas, rawType);
            IEnumerable<T> objects = rawObjects.Select(rawMobj => converter((TRaw)rawMobj, stringSection));
            return objects.Cast<object>().ToList();
        }

        private delegate TRaw ReadBinaryData<out TRaw>(BinaryReader binaryReader, List<SectionRela> relas, long baseOffset);

        private static void ParseRawObjectsOfType<TRaw>(MemoryStream stream, List<object> objects, BinaryReader reader, List<SectionRela> relas, ReadBinaryData<TRaw> readBinaryData)
        {
            while (stream.Position != stream.Length)
            {
                objects.Add(readBinaryData(reader, relas, reader.BaseStream.Position));
            }
        }

    }


}
