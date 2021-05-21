using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;

namespace TOKElfTool
{
    public static class Util
    {
        public static T XamlClone<T>(this T original) where T : class
        {
            if (original == null)
                return null;
            object clone;
            using (MemoryStream stream = new MemoryStream())
            {
                XamlWriter.Save(original, stream);
                stream.Seek(0, SeekOrigin.Begin);
                clone = XamlReader.Load(stream);
            }
            if (clone is T)
                return (T)clone;
            else
                return null;
        }

        public static T Last<T>(this IList<T> source) => source[source.Count - 1];
        public static object Last(this IList source) => source[source.Count - 1];

        private static readonly Regex shortenPathRegex = new Regex(@"[\/\\]");
        private const int MAX_PATH_SEGMENT_AMOUNT = 6;

        public static T PopBack<T>(this IList<T> list)
        {
            T last = list.Last();
            list.RemoveAt(list.Count - 1);
            return last;
        }

        public static string ShortenPath(string path)
        {
            string[] segments = shortenPathRegex.Split(path);
            int length = 0;
            bool ending = false;

            // reverse loop
            for (int i = segments.Length - 1; i >= 0; i--)
            {
                length++;

                if (ending)
                    break;

                if (segments[i] == "romfs" || segments.Length - i >= MAX_PATH_SEGMENT_AMOUNT - 1)
                    ending = true;

            }

            string[] result = new string[length];
            Array.Copy(segments, segments.Length - length, result, 0, length);

            return result.Length < segments.Length ? $@"...\{string.Join(@"\", result)}" : string.Join(@"\", result);
        }
    }
}
