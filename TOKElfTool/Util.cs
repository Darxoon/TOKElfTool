using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
    }
}
