using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class HouseFurnitureUpdatePacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public HouseFurnitureUpdatePacket()
    {
    }

    public HouseFurnitureUpdatePacket(int slot, Guid itemId, int quantity, int x, int y, int direction, string properties)
    {
        Slot = slot;
        ItemId = itemId;
        Quantity = quantity;
        X = x;
        Y = y;
        Direction = direction;
        Properties = properties;
    }

    [Key(0)]
    public int Slot { get; set; }

    [Key(1)]
    public Guid ItemId { get; set; }

    [Key(2)]
    public int Quantity { get; set; }

    [Key(3)]
    public int X { get; set; }

    [Key(4)]
    public int Y { get; set; }

    [Key(5)]
    public int Direction { get; set; }

    [Key(6)]
    public string Properties { get; set; }
}
