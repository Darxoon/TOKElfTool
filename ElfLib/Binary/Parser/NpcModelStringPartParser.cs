using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElfLib.CustomDataTypes;
using ElfLib.CustomDataTypes.Registry;

namespace ElfLib.Binary.Parser
{
    internal class NpcModelStringPartParser<TNew, TSource> : IDataParser where TNew : struct where TSource : struct
    {
        private readonly IDataParser innerParser;
        private readonly Section stringSection;
        
        private readonly SortedDictionary<long, object> additionalPositionalData;

        public NpcModelStringPartParser(Section stringSection, SortedDictionary<long, object> additionalPositionalData, IDataParser innerParser)
        {
            this.innerParser = innerParser;
            this.additionalPositionalData = additionalPositionalData;
            this.stringSection = stringSection;
        }

        public IDictionary<ElfType, List<object>> Parse()
        {
            IDictionary<ElfType, List<object>> dict = innerParser.Parse();
            Dictionary<ElfType, List<object>> result = new Dictionary<ElfType, List<object>>();

            Dictionary<long, object> newPositionalData = new Dictionary<long, object>();
            
            foreach ((long offset, object instance) in additionalPositionalData)
            {
                if (instance is TSource source)
                {
                    const ElfType elfType = ElfType.Main;
                    object converted = Util.RawToNormalObject<TNew, TSource>(source, stringSection);
                    newPositionalData[offset] = converted;
                    if (!result.ContainsKey(elfType))
                        result[elfType] = new List<object>(new object[dict[elfType].Count]);
                    result[elfType][dict[elfType].IndexOf(source)] = converted;
                }
            }

            foreach ((long offset, object instance) in newPositionalData)
            {
                additionalPositionalData[offset] = instance;
            }
            
            // foreach ((ElfType elfType, List<object> instances) in dict)
            // {
            //     result[elfType] = instances.Select(instance => (object)Util.RawToNormalObject<TNew, TSource>((TSource)instance, stringSection)).ToList();
            // }

            return result;
        }

    }
}
