using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class FurnitureStoragePacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public FurnitureStoragePacket()
    {
    }

    public FurnitureStoragePacket(bool close, int slots, FurnitureStorageUpdatePacket[] items)
    {
        Close = close;
        Slots = slots;
        Items = items;
    }

    [Key(0)]
    public bool Close { get; set; }

    [Key(1)]
    public int Slots { get; set; }

    [Key(2)]
    public FurnitureStorageUpdatePacket[] Items { get; set; }
}
