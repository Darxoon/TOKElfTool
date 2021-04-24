using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
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
            List<Symbol> symbolTable = ParseSymbolTable(sections, stringTable);

            Dictionary<Symbol, List<Element<T>>> unmappedData = ParseData<T>(sections, relas, dataType, symbolTable);
            Dictionary<ElfSymbolType, List<Element<T>>> mappedData = MapData<T>(unmappedData, symbolTable, sections, out Dictionary<Symbol, ElfSymbolType> symbolMappings);

            if (verbose)
            {
                #region Log fields
                Trace.WriteLine("Data:");
                Trace.Indent();
                foreach (var item in unmappedData)
                {
                    Trace.WriteLine(item);
                }
                Trace.Unindent();
                #endregion
            }


            reader.Dispose();
            input.Close();

            return new ElfBinary<T>(sections, unmappedData, mappedData, symbolMappings, symbolTable);
        }

        private static Dictionary<ElfSymbolType, List<Element<T>>> MapData<T>(Dictionary<Symbol, List<Element<T>>> data, List<Symbol> symbolTable, List<Section> sections, out Dictionary<Symbol, ElfSymbolType> symbolMappings)
        {
            Section dataSection = GetSection(sections, ".data");
            List<Symbol> symbols = symbolTable.Where(symbol => symbol.Section == dataSection).OrderBy(symbol => symbol.Value).ToList();

            Dictionary<ElfSymbolType, List<Element<T>>> output = new Dictionary<ElfSymbolType, List<Element<T>>>();
            symbolMappings = new Dictionary<Symbol, ElfSymbolType>();

            foreach ((Symbol symbol, List<Element<T>> list) in data)
            {
                ElfSymbolType mapped = symbols.Count > 1 ? (symbols.IndexOf(symbol) == 0 ? ElfSymbolType.MapLinkNodes : ElfSymbolType.MapLink) : ElfSymbolType.Main;
                output.Add(mapped, list);
                symbolMappings.Add(symbol, mapped);
            }

            return output;
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


        private static Dictionary<Symbol, List<Element<T>>> ParseData<T>(List<Section> sections, List<SectionRela> relas,
            GameDataType dataType, List<Symbol> symbolTable)
        {
            Dictionary<Symbol, List<object>> data = ParseData(sections, relas, dataType, symbolTable);
            Dictionary<Symbol, List<Element<T>>> typedData = new Dictionary<Symbol, List<Element<T>>>();

            foreach ((Symbol symbol, List<object> list) in data)
            {
                typedData.Add(symbol, list.Cast<T>().Select(value => new Element<T>(value)).ToList());
            }

            return typedData;
        }

        private static Dictionary<Symbol, List<object>> ParseData(List<Section> sections, List<SectionRela> relas, GameDataType dataType, List<Symbol> symbolTable)
        {
            if (dataType == GameDataType.None)
                return null;

            Section stringSection = GetSection(sections, ".rodata.str1.1");

            switch (dataType)
            {
                case GameDataType.NPC:
                    return ParseObjectsOfType<NPC, RawNPC>(sections, relas, stringSection, symbolTable, GameDataType.RawNPC, NPC.From);
                case GameDataType.Mobj:
                    return ParseObjectsOfType<Mobj, RawMobj>(sections, relas, stringSection, symbolTable, GameDataType.RawMobj, Mobj.From);
                case GameDataType.Aobj:
                    return ParseObjectsOfType<Aobj, RawAobj>(sections, relas, stringSection, symbolTable, GameDataType.RawItem, Aobj.From);
                case GameDataType.BShape:
                    return ParseObjectsOfType<BShape, RawBShape>(sections, relas, stringSection, symbolTable, GameDataType.RawItem, BShape.From);
                case GameDataType.Item:
                    return ParseObjectsOfType<Item, RawItem>(sections, relas, stringSection, symbolTable, GameDataType.RawItem, Item.From);

                default:
                    return ParseRawData(sections, relas, dataType, symbolTable);
            }

        }

        private static Dictionary<Symbol, List<object>> ParseRawData(List<Section> sections, List<SectionRela> relas, GameDataType dataType, List<Symbol> symbolTable)
        {
            Section dataSection = GetSection(sections, ".data");

            List<Symbol> symbols = symbolTable.Where(symbol => symbol.Section == dataSection).OrderBy(symbol => symbol.Value).ToList();
            int symbolIndex = 0;

            Dictionary<Symbol, List<object>> objects = new Dictionary<Symbol, List<object>>();

            byte[] data = dataSection.Content;
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);

            while (stream.Position != stream.Length)
            {
                if (symbols[symbolIndex + 1].Value >= stream.Position)
                {
                    symbolIndex += 1;
                    objects.Add(symbolTable[symbolIndex], new List<object>());
                }

                Symbol currentSymbol = symbolTable[symbolIndex];

                switch (dataType)
                {
                    case GameDataType.RawNPC:
                        ParseRawObjectsOfType(stream, objects[currentSymbol], reader, relas, RawNPC.ReadBinaryData);
                        break;
                    case GameDataType.RawMobj:
                        ParseRawObjectsOfType(stream, objects[currentSymbol], reader, relas, RawMobj.ReadBinaryData);
                        break;
                    case GameDataType.RawAobj:
                        ParseRawObjectsOfType(stream, objects[currentSymbol], reader, relas, RawAobj.ReadBinaryData);
                        break;
                    case GameDataType.RawBShape:
                        ParseRawObjectsOfType(stream, objects[currentSymbol], reader, relas, RawBShape.ReadBinaryData);
                        break;
                    case GameDataType.RawItem:
                        ParseRawObjectsOfType(stream, objects[currentSymbol], reader, relas, RawItem.ReadBinaryData);
                        break;

                    default:
                        throw new ElfParseException("Data type not implemented");
                }
            }



            return objects;
        }

        private delegate T ObjectConverter<out T, in TRaw>(TRaw rawBShape, Section stringSection);

        private static Dictionary<Symbol, List<object>> ParseObjectsOfType<T, TRaw>(List<Section> sections, List<SectionRela> relas, Section stringSection,
            List<Symbol> symbolTable, GameDataType rawType, ObjectConverter<T, TRaw> converter)
        {
            Dictionary<Symbol, List<object>> rawObjects = ParseRawData(sections, relas, rawType, symbolTable);
            Dictionary<Symbol, List<object>> objects = rawObjects
                .Select(pair => new KeyValuePair<Symbol, List<object>>(pair.Key, pair.Value.Select(rawObject => converter((TRaw)rawObject, stringSection)).Cast<object>().ToList()))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            return objects;
        }

        private delegate TRaw ReadBinaryData<out TRaw>(BinaryReader binaryReader, List<SectionRela> relas, long baseOffset);

        private static void ParseRawObjectsOfType<TRaw>(MemoryStream stream, List<object> objects, BinaryReader reader, List<SectionRela> relas, ReadBinaryData<TRaw> readBinaryData)
        {
            objects.Add(readBinaryData(reader, relas, reader.BaseStream.Position));
        }

    }


}
