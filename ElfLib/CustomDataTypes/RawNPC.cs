﻿using System;
using System.Collections.Generic;
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
        public Vector3 position;
        public float rotation;
        public byte appear_flag;
        public byte enemy_flag;
        public byte field_0x2a;
        public byte field_0x2b;
        public int field_0x2c;
        public ElfStringPointer enemy_encounter_str;
        public int field_0x38;
        public int field_0x3c;
        public int collision_flag;
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
        public static RawNPC From(NPC npc, Dictionary<string, ElfStringPointer> stringSectionTable, SortedDictionary<long, ElfStringPointer> stringRelocTable = null, long baseOffset = 0)
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

            return (RawNPC)rawNPC;
        }

    }
}
