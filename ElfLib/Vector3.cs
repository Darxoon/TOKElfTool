using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ElfLib
{
    public struct Vector3
    {
        private static readonly NumberFormatInfo nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = ".",
        };

        public static readonly Vector3 ZERO = new Vector3(0, 0, 0);

        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return $"Vector3({x.ToString(Util.floatFormatString, nfi)}f, {y.ToString(Util.floatFormatString, nfi)}f, {z.ToString(Util.floatFormatString, nfi)}f)";
        }

        /// <summary>
        /// A regex for recognizing strings of the pattern `Vector3(<float>, <float>, <float>)
        /// </summary>
        public static readonly Regex vectorStringRegex = new Regex(@"^Vector3\s*\(\s*(([+-]?\d*\.*\d*f?),\s*([+-]?\d*\.*\d*f?),\s*([+-]?\d*\.*\d*f?))\s*\)$");
        public static Vector3? FromString(string str)
        {
            Match match = vectorStringRegex.Match(str);
            if (match.Success)
            {
                float[] parsedValues = new float[3];
                for (int i = 2; i < match.Groups.Count; i++)
                {
                    var item = match.Groups[i];
                    string rawFloat = item.Value.EndsWith("f") ? item.Value.Substring(0, item.Value.Length - 1) : item.Value;
                    float.TryParse(rawFloat, NumberStyles.Float, nfi, out float parsed);
                    parsedValues[i - 2] = parsed;
                }
                return new Vector3(parsedValues[0], parsedValues[1], parsedValues[2]);
            }
            else
            {
                Console.Error.WriteLine("Error in string");
                return null;
            }
        }
    }
}
