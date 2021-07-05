using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using FieldInfo = System.Reflection.FieldInfo;

namespace TOKElfTool.Search
{
    public class SearchIndex
    {
        private Directory directory;

        private IndexWriter writer;
        private IndexReader reader;

        public IndexReader Reader => reader;

        public SearchIndex(object[] objs)
        {
            const LuceneVersion version = LuceneVersion.LUCENE_48;

            directory = new RAMDirectory();

            Analyzer analyzer = new MyAnalyzer();

            IndexWriterConfig indexConfig = new IndexWriterConfig(version, analyzer);

            writer = new IndexWriter(directory, indexConfig);

            for (int i = 0; i < objs.Length; i++)
            {
                foreach ((string key, string value) in GetAllStrings(objs[i], objs[i].GetType()))
                {
                    if (value != null)
                    {
                        Document document = new Document
                        {
                            new TextField("name", value, Field.Store.YES),
                            new TextField("field", key, Field.Store.YES),
                            new Int32Field("index", i, Field.Store.YES),
                        };

                        writer.AddDocument(document);
                    }
                }
            }

            
            writer.Flush(false, false);

            reader = writer.GetReader(true);
        }

        ~SearchIndex()
        {
            writer?.Dispose();
            reader?.Dispose();
            directory?.Dispose();
        }

        private static Dictionary<string, string> GetAllStrings(object obj, Type type)
        {
            Dictionary<string, string> strings = new Dictionary<string, string>();
            FieldInfo[] fields = type.GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(string))
                {
                    strings.Add(fields[i].Name, (string)fields[i].GetValue(obj));
                }
            }

            return strings;
        }
    }
}
