using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Collections.Slotting;
using Newtonsoft.Json;

namespace Intersect.Server.Database.PlayerData.Players;

/// <summary>
/// Represents a storage container furniture with its own inventory slots.
/// </summary>
public partial class FurnitureStorage
{
    public FurnitureStorage()
    {
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public Guid Id { get; private set; }

    /// <summary>
    /// The furniture slot this storage belongs to.
    /// </summary>
    [JsonIgnore]
    public Guid FurnitureSlotId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(FurnitureSlotId))]
    public virtual HouseFurnitureSlot FurnitureSlot { get; set; }

    /// <summary>
    /// Storage slots for this furniture container.
    /// </summary>
    [JsonIgnore]
    public virtual List<FurnitureStorageSlot> Slots { get; set; } = new List<FurnitureStorageSlot>();
}

/// <summary>
/// Represents an item slot within a furniture storage container.
/// </summary>
public partial class FurnitureStorageSlot : Item, ISlot
{
    public static FurnitureStorageSlot Create(int slotIndex) => new(slotIndex);

    public FurnitureStorageSlot()
    {
    }

    public FurnitureStorageSlot(int slot)
    {
        Slot = slot;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public Guid Id { get; private set; }

    [JsonIgnore]
    public bool IsEmpty => ItemId == default;

    [JsonIgnore]
    public Guid FurnitureStorageId { get; private set; }

    [JsonIgnore]
    [ForeignKey(nameof(FurnitureStorageId))]
    public virtual FurnitureStorage FurnitureStorage { get; private set; }

    public int Slot { get; private set; }
}
