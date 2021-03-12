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

    /// <summary>
    /// An elf binary loaded into memory. Can be obtained through <see cref="ElfParser"/> and be serialized using ElfSerializer.
    /// </summary>
    /// <typeparam name="T">The type of the data which this file holds</typeparam>
    public class ElfBinary<T>
    {

        public virtual List<Section> Sections { get; }
        public virtual List<Element<T>> Data { get; set; }

        public virtual Section GetSection(string name)
        {
            return Sections.Find(value => value.Name == name);
        }

        internal ElfBinary(List<Section> sections, List<Element<T>> data)
        {
            Sections = sections;
            Data = data;
        }
    }
}
