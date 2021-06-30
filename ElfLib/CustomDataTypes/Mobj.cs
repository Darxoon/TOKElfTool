﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;

namespace ElfLib
{
	[StructLayout(LayoutKind.Sequential)]
	[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
	public struct Mobj
    {
		public string level_str;
		public string obj_str;
		public string shape_str;
		public Vector3 position;
		public Vector3 rotation;
        #region Misc and unknown fields
        public byte unk_bool_30;
		public byte field_31;
		public byte field_32;
		public byte field_33;
		public int field_34;
		public int field_38;
		public int field_3C;
		public int field_40;
		public int field_44;
		public int field_48;
		public int field_4C;
		public int field_50;
		public int field_54;
		public int field_58;
		public int field_5C;
		public int field_60;
		public int field_64;
		public int field_68;
		public int field_6C;
		public int field_70;
		public int field_74;
		public int field_78;
		public int field_7C;
		public string field_80;
		public int field_88;
		public int field_8C;
		public int field_90;
		public int field_94;
		public int field_98;
		public int field_9C;
		public int field_A0;
		public int field_A4;
		public int field_A8;
		public float unk_AC;
		public int field_B0;
		public int field_B4;
		public int field_B8;
		public int field_BC;
		public int field_C0;
		public int field_C4;
		public int field_C8;
		public int field_CC;
		public int field_D0;
		public int field_D4;
		public float unk_D8;
		public int field_DC;
		public int field_E0;
		public int field_E4;
		public int field_E8;
		public int field_EC;
		public int field_F0;
		public int field_F4;
		public string init_function_str;
		public int field_100;
		public int field_104;
		public int field_108;
		public int field_10C;
		public int field_110;
		public int field_114;
		public int field_118;
		public int field_11C;
		public int field_120;
		public int field_124;
		public int field_128;
		public int field_12C;
		public int field_130;
		public int field_134;
		public int field_138;
		public int field_13C;
		public int field_140;
		public int field_144;
		public int field_148;
		public int field_14C;
		public int field_150;
		public int field_154;
		public int field_158;
		public int field_15C;
		public int field_160;
		public int field_164;
		public int field_168;
		public int field_16C;
		public int field_170;
		public int field_174;
        #endregion



        internal static Mobj From(RawMobj rawNpc, Section stringSection)
        {
            object npc = new Mobj();
            foreach (FieldInfo npcField in typeof(Mobj).GetFields())
            {
                FieldInfo rawNpcField = typeof(RawMobj).GetField(npcField.Name);
                //Trace.WriteLine($"NPC: {npcField.Name}: {npcField.FieldType.Name}, \tRawNPC: {rawNpcField.Name}: {rawNpcField.FieldType.Name}");
                if (npcField.FieldType == typeof(string))
                {
                    Trace.WriteLine("Peter bghjmnv ");
                    Trace.WriteLine(rawNpcField.Name);
                    Trace.WriteLine(rawNpcField.GetValue(rawNpc));
                    string str = stringSection.GetString((ElfStringPointer)rawNpcField.GetValue(rawNpc));
                    npcField.SetValue(npc, str);
                }
                else if (npcField.FieldType.BaseType == typeof(Enum))
                {
                    npcField.SetValue(npc, (int)rawNpcField.GetValue(rawNpc));
                }
                else if (npcField.FieldType == typeof(bool) && rawNpcField.FieldType == typeof(int))
                {
                    npcField.SetValue(npc, (int)rawNpcField.GetValue(rawNpc) > 0);
                }
                else if (npcField.FieldType == rawNpcField.FieldType)
                {
                    npcField.SetValue(npc, rawNpcField.GetValue(rawNpc));
                }
                else
                    throw new Exception($"Internal error: NPC field {npcField} {rawNpcField.FieldType} and RawNPC field {rawNpcField} types don't match");
            }

            return (Mobj)npc;
        }
    }
}
