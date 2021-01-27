using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace ElfLib
{
    public enum GameDataType
    {
        None,
        NPC,
        RawNPC, // TODO: temporary
        Mobj,
        // TODO: Add more
    }

    public static class ElfParser
    {
        const int HEADER_LENGTH = 0x40;

        public static ElfBinary<T> ParseFile<T>(string filepath, GameDataType dataType, bool verbose = true)
        {
            FileStream input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, (int)new FileInfo(filepath).Length);
            BinaryReader reader = new BinaryReader(input);

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
            sections.Find(value => value.Name == name);

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
            if (dataSection == null || stringSection == null)
                throw new Exception("Couldn't find .data or .rodata.str1.1 section");

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
                        objects.Add(RawNPC.ReadBinaryData(reader, relas));
                    }
                    break;
                default:
                    break;
            }

            return objects;
        }

    }


}
