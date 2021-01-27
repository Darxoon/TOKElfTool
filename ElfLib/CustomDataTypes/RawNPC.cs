using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib
{
#pragma warning disable IDE0051, IDE0044
    [StructLayout(LayoutKind.Sequential)]
    public struct RawNPC
    {
        public ElfStringPointer level_str;
        public ElfStringPointer obj_str;
        public ElfStringPointer shape_str;
        public Vector3 pos;
        public float field_0x24;
        public int field_0x28;
        public int field_0x2c;
        public ElfStringPointer ukn_str;
        public int field_0x38;
        public int field_0x3c;
        public int field_0x40;
        public int field_0x44;
        public float field_0x48;
        public float field_0x4c;
        public int field_0x50;
        public int field_0x54;
        public int field_0x58;
        public int field_0x5c;
        public float field_0x60;
        public float field_0x64;
        public float field_0x68;
        public float field_0x6c;
        public float field_0x70;
        public float field_0x74;
        public float field_0x78;
        public int field_0x7c;
        public int field_0x80;
        public int field_0x84;
        public float field_0x88;
        public float field_0x8c;
        public float field_0x90;
        public float field_0x94;
        public float field_0x98;
        public float field_0x9c;
        public float field_0xa0;
        public int field_0xa4;
        public int field_0xa8;
        public int field_0xac;
        public int field_0xb0;
        public int field_0xb4;
        public float field_0xb8;
        public int field_0xbc;
        public int field_0xc0;
        public int field_0xc4;
        public ElfStringPointer init_function_str;
        public int field_0xd0;
        public int field_0xd4;
        public int field_0xd8;
        public int field_0xdc;
        public ElfStringPointer talk_function_str;
        public ElfStringPointer action_function_str;
        public int field_0xec;
        public int field_0xf0;
        public int field_0xf4;
        public int field_0xf8;

        public override string ToString()
        {
            Type type = GetType();
            FieldInfo[] fields = type.GetFields();
            PropertyInfo[] properties = type.GetProperties();
            RawNPC rawNpc = this;

            Dictionary<string, object> values = new Dictionary<string, object>();
            Array.ForEach(fields, (field) => values.Add(field.Name, field.GetValue(rawNpc)));
            Array.ForEach(properties, (property) =>
            {
                if (property.CanRead)
                    values.Add(property.Name, property.GetValue(rawNpc, null));
            });

            return $"RawNPC{{{string.Join(", ", values.Select((key) => $"{key.Key}: {key.Value}").Take(5))} + {Math.Max(values.Count - 5, 0)} more";
        }

        internal static RawNPC ReadBinaryData(BinaryReader binaryReader, List<SectionRela> relas)
        {
            RawNPC rawNPC = Util.FromBinaryReader<RawNPC>(binaryReader);
            rawNPC.level_str = ElfStringPointer.ResolveRelocation(relas, 0);
            rawNPC.obj_str = ElfStringPointer.ResolveRelocation(relas, 8);
            rawNPC.shape_str = ElfStringPointer.ResolveRelocation(relas, 16);
            rawNPC.ukn_str = ElfStringPointer.ResolveRelocation(relas, 48);
            rawNPC.init_function_str = ElfStringPointer.ResolveRelocation(relas, 200);
            rawNPC.talk_function_str = ElfStringPointer.ResolveRelocation(relas, 224);
            rawNPC.action_function_str = ElfStringPointer.ResolveRelocation(relas, 232);
            return rawNPC;
        }

        private static long GetRelocatableAddress(long offset)
        {
            return 0;
        }

        /// <summary>
        /// Creates a RawNPC from an NPC by copying everything except for the strings, which it looks up in the `stringSectionTable` and adds to the `stringRelocTable`.
        /// </summary>
        /// <param name="npc">The source NPC</param>
        /// <param name="stringSectionTable">The table where the string pointers are stored</param>
        /// <param name="stringRelocTable">The table it adds the string pointer references to, for relocation. Can be left as null</param>
        /// <returns>The output RawNPC</returns>
        public static RawNPC FromNPC(NPC npc, Dictionary<string, ElfStringPointer> stringSectionTable, SortedDictionary<long, ElfStringPointer> stringRelocTable = null)
        {
            object rawNPC = new RawNPC();

            foreach (FieldInfo rawNpcField in typeof(RawNPC).GetFields())
            {
                FieldInfo npcField = typeof(NPC).GetField(rawNpcField.Name);
                if (npcField == null)
                    throw new Exception($"Didn't find field `{rawNpcField.Name}` in type NPC");

                if (rawNpcField.FieldType == typeof(ElfStringPointer) && npcField.FieldType == typeof(string))
                {
                    string str = (string)npcField.GetValue(npc);
                    ElfStringPointer stringPointer = str != null ? stringSectionTable[str] : ElfStringPointer.NULL;
                    if (stringRelocTable != null)
                        stringRelocTable.Add(rawNpcField.GetFieldOffset(), stringPointer);
                    else
                        rawNpcField.SetValue(rawNPC, stringPointer);
                }
                else if (npcField.FieldType == rawNpcField.FieldType)
                {
                    rawNpcField.SetValue(rawNPC, npcField.GetValue(npc));
                }
                else
                    throw new Exception($"Types `{npcField.FieldType}` and `{rawNpcField.FieldType}` didn't match on field `{npcField.Name}`");
            }

            return (RawNPC)rawNPC;
        }

    }
}
