using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElfLib.Binary.Parser;
using ElfLib.Types.Disposition;
using ElfLib.Types.Registry;

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

        public static ElfBinary<T> ParseFile<T>(string filepath, GameDataType dataType)
        {
            FileStream input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, (int)new FileInfo(filepath).Length);
            BinaryReader reader = new BinaryReader(input);

            return ParseFile<T>(reader, dataType);

        }

        public static ElfBinary<T> ParseFile<T>(BinaryReader reader, GameDataType dataType)
        {
            Stream input = reader.BaseStream;

            // get constants from header
            input.Position = 0x28;
            int sectionHeaderTableOffset = (int)reader.ReadInt64();
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

            List<SectionRela> relas = ParseRelocations(GetSection(sections, ".rela.data"));

            List<Symbol> symbolTable = ParseSymbolTable(sections, stringTable);

            Dictionary<ElfType, List<Element<T>>> data = ParseData<T>(sections, relas, dataType, symbolTable,
                out Dictionary<ElfType, List<long>> dataOffsets);


            input.Dispose();

            return new ElfBinary<T>
            {
                Data = data,
                DataOffsets = dataOffsets,
                Sections = sections,
                SymbolTable = symbolTable,
            };
        }


        private static List<Symbol> ParseSymbolTable(List<Section> sections, Section stringTable)
        {
            Section symbolTableSection = GetSection(sections, ".symtab");
            BinaryReader reader = symbolTableSection.CreateBinaryReader();
            Stream stream = reader.BaseStream;

            List<Symbol> output = new List<Symbol>();

            while (stream.Position != stream.Length)
            {
                output.Add(new Symbol(reader, stringTable, sections));
            }

            return output;
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

        private static List<SectionRela> ParseRelocations(Section relaSection)
        {
            List<SectionRela> relas = new List<SectionRela>();

            BinaryReader reader = relaSection.CreateBinaryReader();
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                relas.Add(SectionRela.FromBinary(reader));
            }

            reader.Dispose();

            return relas;
        }


        private static Dictionary<ElfType, List<Element<T>>> ParseData<T>(List<Section> sections, 
            List<SectionRela> relas, GameDataType dataType, List<Symbol> symbolTable, out Dictionary<ElfType, List<long>> dataOffsets)
        {
            (IDictionary<ElfType, List<object>> data, Dictionary<ElfType, List<long>> offsets) = ParseData(sections, relas, dataType, symbolTable);
            Dictionary<ElfType, List<Element<T>>> typedData = new Dictionary<ElfType, List<Element<T>>>();

            foreach ((ElfType type, List<object> instances) in data)
            {
                typedData[type] = instances
                    .Select(instance => new Element<T>((T)instance))
                    .ToList();
            }

            dataOffsets = offsets;
            
            return typedData;
        }

        private static (IDictionary<ElfType, List<object>>, Dictionary<ElfType, List<long>> offsets) ParseData(List<Section> sections, List<SectionRela> relas, 
            GameDataType dataType, List<Symbol> symbolTable)
        {
            if (dataType == GameDataType.None)
                return (null, null);

            Section dataSection = GetSection(sections, ".data");
            Section rodataSection = GetSection(sections, ".rodata");
            Section stringSection = GetSection(sections, ".rodata.str1.1");

            Dictionary<ElfType, List<long>> dataOffsets = null;

            IDataParser parser = dataType switch
            {
                GameDataType.NPC => Parse<Npc, RawNpc>(sections, relas, out dataOffsets),
                
                GameDataType.Mobj => Parse<Mobj, RawMobj>(sections, relas, out dataOffsets),
                
                GameDataType.Aobj => Parse<Aobj, RawAobj>(sections, relas, out dataOffsets),
                
                GameDataType.BShape => Parse<BShape, RawBShape>(sections, relas, out dataOffsets),
                
                GameDataType.Item => Parse<Item, RawItem>(sections, relas, out dataOffsets),
                
                GameDataType.Maplink => new MaplinkParser(symbolTable, dataSection, stringSection, relas, out dataOffsets),
                
                GameDataType.DataNpc => Parse<NpcType, RawNpcType>(sections, relas, out dataOffsets),
                
                GameDataType.DataItem => Parse<ItemType, RawItemType>(sections, relas, out dataOffsets),
                
                GameDataType.DataNpcModel => new NpcModelParser(stringSection, dataSection, rodataSection, out dataOffsets,
                    relas, ParseRelocations(GetSection(sections, ".rela.rodata"))),
            };

            return (parser.Parse(), dataOffsets);
        }

        private static StringDataParser<T1, T2> Parse<T1, T2>(List<Section> sections, List<SectionRela> relocationTable,
            out Dictionary<ElfType, List<long>> dataOffsets) 
            where T1 : struct where T2 : struct
        {
            Section dataSection = GetSection(sections, ".data");
            Section stringSection = GetSection(sections, ".rodata.str1.1");

            return new StringDataParser<T1, T2>(stringSection, new SimpleDataParser<T2>(dataSection, relocationTable, out dataOffsets));
        }
        
    }

}
