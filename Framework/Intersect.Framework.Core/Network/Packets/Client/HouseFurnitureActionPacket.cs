using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class HouseFurnitureActionPacket : IntersectPacket
{
    public enum ActionType
    {
        Place = 0,
        Remove = 1,
        Move = 2
    }

    //Parameterless Constructor for MessagePack
    public HouseFurnitureActionPacket()
    {
    }

    public HouseFurnitureActionPacket(ActionType action, int inventorySlot, int furnitureSlot, int x, int y, int direction)
    {
        Action = action;
        InventorySlot = inventorySlot;
        FurnitureSlot = furnitureSlot;
        X = x;
        Y = y;
        Direction = direction;
    }

    [Key(0)]
    public ActionType Action { get; set; }

    [Key(1)]
    public int InventorySlot { get; set; }

    [Key(2)]
    public int FurnitureSlot { get; set; }

    [Key(3)]
    public int X { get; set; }

    [Key(4)]
    public int Y { get; set; }

    [Key(5)]
    public int Direction { get; set; }
}
