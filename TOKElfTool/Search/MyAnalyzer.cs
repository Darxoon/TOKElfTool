using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using J2N;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace TOKElfTool.Search
{
    public class MyAnalyzer : Analyzer
    {
        public sealed class MyTokenizer : LetterTokenizer
        {
            public MyTokenizer(LuceneVersion matchVersion, TextReader input) : base(matchVersion, input)
            {
            }

            public MyTokenizer(LuceneVersion matchVersion, AttributeFactory factory, TextReader input) : base(matchVersion, factory, input)
            {
            }

            protected override bool IsTokenChar(int c) => Character.IsLetter(c) || (char)c == '_';
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            const LuceneVersion version = LuceneVersion.LUCENE_48;

            Tokenizer baseTokenizer = new MyTokenizer(version, reader);
            StandardFilter standardFilter = new StandardFilter(version, baseTokenizer);
            WordDelimiterFilter wordDelimiterFilter = new WordDelimiterFilter(version, standardFilter,
                WordDelimiterFlags.CATENATE_WORDS | WordDelimiterFlags.GENERATE_WORD_PARTS |
                WordDelimiterFlags.PRESERVE_ORIGINAL | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE, CharArraySet.EMPTY_SET);
            LowerCaseFilter lcFilter = new LowerCaseFilter(version, wordDelimiterFilter);
            return new TokenStreamComponents(baseTokenizer, lcFilter);
        }

    }
}
