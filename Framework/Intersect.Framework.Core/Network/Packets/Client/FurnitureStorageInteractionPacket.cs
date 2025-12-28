using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class FurnitureStorageInteractionPacket : IntersectPacket
{
    public enum InteractionType
    {
        Deposit = 0,
        Withdraw = 1,
        Close = 2
    }

    //Parameterless Constructor for MessagePack
    public FurnitureStorageInteractionPacket()
    {
    }

    public FurnitureStorageInteractionPacket(InteractionType type, int inventorySlot, int storageSlot, int quantity)
    {
        Type = type;
        InventorySlot = inventorySlot;
        StorageSlot = storageSlot;
        Quantity = quantity;
    }

    [Key(0)]
    public InteractionType Type { get; set; }

    [Key(1)]
    public int InventorySlot { get; set; }

    [Key(2)]
    public int StorageSlot { get; set; }

    [Key(3)]
    public int Quantity { get; set; }
}
