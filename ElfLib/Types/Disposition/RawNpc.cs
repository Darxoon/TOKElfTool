using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Disposition
{
#pragma warning disable IDE0051, IDE0044
    [StructLayout(LayoutKind.Sequential)]
    public struct RawNpc
    {
        public Pointer level_str;
        public Pointer obj_str;
        public Pointer shape_str;
        public Vector3 position;
        public float rotation;
        public byte appear_flag;
        public byte enemy_flag;
        public byte field_0x2a;
        public byte field_0x2b;
        public int field_0x2c;
        public Pointer enemy_encounter_str;
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
        public Vector3 walk_origin;
        public Vector3 walk_distance;
        public float field_0x78;
        public int field_0x7c;
        public int field_0x80;
        public int field_0x84;
        public Vector3 chase_origin;
        public Vector3 chase_distance;
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
        public Pointer init_function_str;
        public int field_0xd0;
        public int field_0xd4;
        public int field_0xd8;
        public int field_0xdc;
        public Pointer talk_function_str;
        public Pointer action_function_str;
        public int field_0xec;
        public int field_0xf0;
        public int field_0xf4;
        public int field_0xf8;

        public override string ToString()
        {
            Type type = GetType();
            FieldInfo[] fields = type.GetFields();
            PropertyInfo[] properties = type.GetProperties();
            RawNpc rawNpc = this;

            Dictionary<string, object> values = new Dictionary<string, object>();
            Array.ForEach(fields, (field) => values.Add(field.Name, field.GetValue(rawNpc)));
            Array.ForEach(properties, (property) =>
            {
                if (property.CanRead)
                    values.Add(property.Name, property.GetValue(rawNpc, null));
            });

            return $"RawNPC{{{string.Join(", ", values.Select((key) => $"{key.Key}: {key.Value}").Take(5))} + {Math.Max(values.Count - 5, 0)} more";
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
        public static RawNpc From(Npc npc, Dictionary<string, Pointer> stringSectionTable, SortedDictionary<long, Pointer> stringRelocTable = null, long baseOffset = 0)
        {
            object rawNPC = new RawNpc();

            foreach (FieldInfo rawNpcField in typeof(RawNpc).GetFields())
            {
                FieldInfo npcField = typeof(Npc).GetField(rawNpcField.Name);
                if (npcField == null)
                    throw new Exception($"Didn't find field `{rawNpcField.Name}` in type NPC");

                if (rawNpcField.FieldType == typeof(Pointer) && npcField.FieldType == typeof(string))
                {
                    string str = (string)npcField.GetValue(npc);
                    Pointer stringPointer = str != null ? stringSectionTable[str] : Pointer.NULL;
                    if (stringRelocTable != null)
                        stringRelocTable.Add(rawNpcField.GetFieldOffset() + baseOffset, stringPointer);
                    else
                        rawNpcField.SetValue(rawNPC, stringPointer);
                }
                else if (npcField.FieldType.BaseType == typeof(Enum))
                {
                    rawNpcField.SetValue(rawNPC, npcField.GetValue(npc));
                }
                else if (npcField.FieldType == typeof(bool))
                {
                    rawNpcField.SetValue(rawNPC, (bool)npcField.GetValue(npc) ? 1 : 0);
                }
                else if (npcField.FieldType == rawNpcField.FieldType)
                {
                    rawNpcField.SetValue(rawNPC, npcField.GetValue(npc));
                }
                else
                    throw new Exception($"Types `{npcField.FieldType}` and `{rawNpcField.FieldType}` didn't match on field `{npcField.Name}`");
            }

            return (RawNpc)rawNPC;
        }

    }
}
