using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ElfLib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NPC
    { /* An interactable character */
        public string level_str;
        public string obj_str;
        public string shape_str;
        public Vector3 pos;
        public float field_0x24;
        public int field_0x28;
        public int field_0x2c;
        public string enemy_encounter_str;
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
            NPC rawNpc = this;

            Dictionary<string, object> values = new Dictionary<string, object>();
            Array.ForEach(fields, (field) => values.Add(field.Name, field.GetValue(rawNpc)));
            Array.ForEach(properties, (property) =>
            {
                if (property.CanRead)
                    values.Add(property.Name, property.GetValue(rawNpc, null));
            });

            return $"NPC{{{string.Join(", ", values.Select((key) => $"{key.Key}: {key.Value}").Take(5))} + {Math.Max(values.Count - 5, 0)} more";
        }

        internal static NPC From(RawNPC rawNpc, Section stringSection)
        {
            object npc = new NPC();
            Trace.WriteLine("Loading NPC from RawNPC");
            Trace.Indent();
            foreach (FieldInfo npcField in typeof(NPC).GetFields())
            {
                FieldInfo rawNpcField = typeof(RawNPC).GetField(npcField.Name);
                //Trace.WriteLine($"NPC: {npcField.Name}: {npcField.FieldType.Name}, \tRawNPC: {rawNpcField.Name}: {rawNpcField.FieldType.Name}");
                if (npcField.FieldType == typeof(string))
                {
                    string str = stringSection.GetString((ElfStringPointer)rawNpcField.GetValue(rawNpc));
                    Trace.WriteLine(str, npcField.Name);
                    npcField.SetValue(npc, str);
                    Trace.WriteLine(npcField.GetValue(npc));
                }
                else if (npcField.FieldType == rawNpcField.FieldType)
                {
                    npcField.SetValue(npc, rawNpcField.GetValue(rawNpc));
                }
                else
                    throw new Exception($"Internal error: NPC field {npcField} and RawNPC field {rawNpcField} types don't match");
            }
            Trace.Unindent();

            return (NPC)npc;
        }
    }
}
