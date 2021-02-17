using System;
using System.Collections.Generic;
using System.Text;

namespace ElfLib.CustomDataTypes.NPC
{
    public enum IsEnemyFlags : int
    {
        [EnumMetadata(DisplayName = "0 (No)")]
        No = 0,
        [EnumMetadata(DisplayName = "1 (?)")]
        Unused = 1,
        [EnumMetadata(DisplayName = "256 (Yes)")]
        Yes = 256,
        [EnumMetadata(DisplayName = "257 (Yes, ?)")]
        Yes2 = 257,
    }
}
