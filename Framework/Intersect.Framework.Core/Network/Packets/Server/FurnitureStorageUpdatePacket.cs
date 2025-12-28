using Intersect.Framework.Core.GameObjects.Items;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class FurnitureStorageUpdatePacket : InventoryUpdatePacket
{
    //Parameterless Constructor for MessagePack
    public FurnitureStorageUpdatePacket() : base(0, Guid.Empty, 0, null, null)
    {
    }

    public FurnitureStorageUpdatePacket(int slot, Guid id, int quantity, Guid? bagId, ItemProperties properties) : base(
        slot, id, quantity, bagId, properties
    )
    {
    }
}
