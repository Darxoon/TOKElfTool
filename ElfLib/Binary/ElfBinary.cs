using System.Collections.Generic;

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

    public enum ElfType
    {
        Main,
        MaplinkHeader,
        Files,
        State,
        SubStates,
        Face,
        Anime,
    }

    /// <summary>
    /// An elf binary loaded into memory. Can be obtained through <see cref="ElfParser"/> and be serialized using ElfSerializer.
    /// </summary>
    /// <typeparam name="T">The type of the data which this file holds</typeparam>
    public sealed class ElfBinary<T>
    {
        public List<Symbol> SymbolTable { get; internal set; }
        public List<Section> Sections { get; internal set; }
        public Dictionary<ElfType, List<Element<T>>> Data { get; internal set; }
        public Dictionary<ElfType, List<long>> DataOffsets { get; internal set; }

        public Section GetSection(string name)
        {
            return Sections.Find(value => value.Name == name);
        }
    }
}
