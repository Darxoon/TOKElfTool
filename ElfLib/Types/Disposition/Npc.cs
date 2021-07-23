using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ElfLib.Types.Disposition
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Npc
    { /* An interactable character */
        public string level_str;
        public string obj_str;
        public string shape_str;
        public Vector3 position;
        public float rotation;
        public byte appear_flag;
        public byte enemy_flag;
        public byte field_0x2a;
        public byte field_0x2b;
        public int field_0x2c;
        public string enemy_encounter_str;
        public int field_0x38;
        public int field_0x3c;
        public bool collision_flag;
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
        public string init_function_str;
        public int field_0xd0;
        public int field_0xd4;
        public int field_0xd8;
        public int field_0xdc;
        public string talk_function_str;
        public string action_function_str;
        public int field_0xec;
        public int field_0xf0;
        public int field_0xf4;
        public int field_0xf8;

        public override string ToString()
        {
            Type type = GetType();
            FieldInfo[] fields = type.GetFields();
            PropertyInfo[] properties = type.GetProperties();
            Npc rawNpc = this;

            Dictionary<string, object> values = new Dictionary<string, object>();
            Array.ForEach(fields, (field) => values.Add(field.Name, field.GetValue(rawNpc)));
            Array.ForEach(properties, (property) =>
            {
                if (property.CanRead)
                    values.Add(property.Name, property.GetValue(rawNpc, null));
            });

            return $"NPC{{{string.Join(", ", values.Select((key) => $"{key.Key}: {key.Value}").Take(5))} + {Math.Max(values.Count - 5, 0)} more";
        }
    }
}
