﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ElfLib.Binary.Parser
{
    internal class StringArrayParser<TNew, TSource> : IDataParser where TNew : struct where TSource : struct
    {
        private readonly IDataParser innerParser;
        private readonly Section stringSection;

        public delegate TNew ObjectConverter(TSource rawBShape, Section stringSection);

        public StringArrayParser(Section stringSection, IDataParser innerParser)
        {
            this.innerParser = innerParser;
            this.stringSection = stringSection;
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            IDictionary<ElfType, List<object>> dict = innerParser.Parse();
            Dictionary<ElfType, List<object>> result = new Dictionary<ElfType, List<object>>();

            foreach ((ElfType elfType, List<object> instances) in dict)
            {
                result[elfType] = instances.Select(instance => instance is object[] source
                    ? source.Select(instance => (object)Util.RawToNormalObject<TNew, TSource>((TSource)instance, stringSection)).ToList()
                    : (object)Util.RawToNormalObject<TNew, TSource>((TSource)instance, stringSection)).ToList();
            }

            return result;
        }

    }
}
