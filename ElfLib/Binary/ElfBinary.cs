using System;
using System.Collections.Generic;
using System.Linq;

namespace ElfLib
{
    public class Element<T>
    {
        public T value;

        public Element(T value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return $"Element({value})";
        }
    }

    public enum ElfSymbolType
    {
        Main,
        MapLinkNodes,
        MapLink,
    }

    /// <summary>
    /// An elf binary loaded into memory. Can be obtained through <see cref="ElfParser"/> and be serialized using ElfSerializer.
    /// </summary>
    /// <typeparam name="T">The type of the data which this file holds</typeparam>
    public sealed class ElfBinary<T>
    {

        public List<Symbol> SymbolTable { get; }
        public List<Section> Sections { get; }
        public Dictionary<Symbol, ElfSymbolType> SymbolTypes { get; }
        public Dictionary<Symbol, List<Element<T>>> UnmappedData { get; }
        public Dictionary<ElfSymbolType, List<Element<T>>> Data { get; }

        public Section GetSection(string name)
        {
            return Sections.Find(value => value.Name == name);
        }

        internal ElfBinary(
            List<Section> sections,
            Dictionary<Symbol, List<Element<T>>> unmappedData,
            Dictionary<ElfSymbolType, List<Element<T>>> data,
            Dictionary<Symbol, ElfSymbolType> symbolTypes,
            List<Symbol> symbolTable)
        {
            Sections = sections;
            UnmappedData = unmappedData;
            Data = data;
            SymbolTypes = symbolTypes;
            SymbolTable = symbolTable;
        }
    }
}
