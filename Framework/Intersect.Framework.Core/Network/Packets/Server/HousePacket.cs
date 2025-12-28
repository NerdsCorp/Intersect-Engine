using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class HousePacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public HousePacket()
    {
    }

    public HousePacket(bool close, Guid houseId, Guid ownerId, int furnitureSlots, HouseFurnitureUpdatePacket[] furniture)
    {
        Close = close;
        HouseId = houseId;
        OwnerId = ownerId;
        FurnitureSlots = furnitureSlots;
        Furniture = furniture;
    }

    [Key(0)]
    public bool Close { get; set; }

    [Key(1)]
    public Guid HouseId { get; set; }

    [Key(2)]
    public Guid OwnerId { get; set; }

    [Key(3)]
    public int FurnitureSlots { get; set; }

    [Key(4)]
    public HouseFurnitureUpdatePacket[] Furniture { get; set; }
}
