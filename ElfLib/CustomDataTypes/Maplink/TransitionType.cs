using System;
using System.Collections.Generic;
using System.Text;

namespace ElfLib.CustomDataTypes.Maplink
{
    [StringEnum]
    public enum TransitionType
    {
        [EnumMetadata(DisplayName = "null", Identifier = null)]
        Null,
        [EnumMetadata(DisplayName = "Normal Transition (\"ベロ\")", Identifier = "ベロ")]
        Bero,
        [EnumMetadata(DisplayName = "Event (\"イベント\")", Identifier = "イベント")]
        Event,
        [EnumMetadata(DisplayName = "Door (\"ドア\")", Identifier = "ドア")]
        Door,
        [EnumMetadata(DisplayName = "Pipe (below) (\"土管：下\")", Identifier = "土管：下")]
        PipeBelow,
    }
}
