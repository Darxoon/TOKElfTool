using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElfLib.CustomDataTypes;
using ElfLib.CustomDataTypes.Registry;

namespace ElfLib.Binary.Parser
{
    internal class StringDataParser<TNew, TSource> : IDataParser
    {
        private readonly IDataParser innerParser;
        private readonly ObjectConverter converter;
        private readonly Section stringSection;

        public delegate TNew ObjectConverter(TSource rawBShape, Section stringSection);

        public StringDataParser(ObjectConverter converter, Section stringSection, IDataParser innerParser)
        {
            this.innerParser = innerParser;
            this.stringSection = stringSection;
            this.converter = converter;
        }

        public Dictionary<ElfType, List<object>> Parse()
        {
            Dictionary<ElfType, List<object>> dict = innerParser.Parse();
            Dictionary<ElfType, List<object>> result = new Dictionary<ElfType, List<object>>();

            foreach ((ElfType elfType, List<object> instances) in dict)
            {
                // TODO: make everything more generic
                result[elfType] = instances.Select(instance => (object)converter((TSource)instance, stringSection)).ToList();
            }

            return result;
        }

    }
}
