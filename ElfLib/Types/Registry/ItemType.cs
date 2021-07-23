using System.Runtime.InteropServices;

namespace ElfLib.Types.Registry
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ItemType
    {
        public string id;
        public string name;
        public string type;
        public string model_internal_id;
        public int field_0x20;
        public int field_0x24;
        public int field_0x28;
        public int field_0x2c;
        public int field_0x30;
        public int buy_price;
        public int sell_price;
        public int field_0x3c;
        public int value;
        public int field_0x44;
        public string model_base_path;
        public string field_0x50;
        public string description_id;
        public string icon_id;
        public int field_0x68;
        public int field_0x6c;
        public string field_0x70;
        public string field_0x78;
        public int field_0x80;
        public int field_0x84;
        public string field_0x88;
        public string field_0x90;
        public string field_0x98;
        public string field_0xa0;
        public int field_0xa8;
        public int field_0xac;
        public int field_0xb0;
        public int field_0xb4;
        public int field_0xb8;
        public int field_0xbc;
        public int field_0xc0;
        public int field_0xc4;
        public string script_id;

        public static ItemType From(RawItemType itemType, Section stringSection) => Util.RawToNormalObject<ItemType, RawItemType>(itemType, stringSection);
    }
}
