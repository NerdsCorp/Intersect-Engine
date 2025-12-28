using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Collections.Slotting;
using Newtonsoft.Json;

namespace Intersect.Server.Database.PlayerData.Players;

/// <summary>
/// Represents a furniture slot in a player's house.
/// Stores both the item and its position on the map.
/// </summary>
public partial class HouseFurnitureSlot : Item, ISlot
{
    public static HouseFurnitureSlot Create(int slotIndex) => new(slotIndex);

    public HouseFurnitureSlot()
    {
    }

    public HouseFurnitureSlot(int slot)
    {
        Slot = slot;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public Guid Id { get; private set; }

    [JsonIgnore]
    public bool IsEmpty => ItemId == default;

    [JsonIgnore]
    public Guid HouseId { get; private set; }

    [JsonIgnore]
    [ForeignKey(nameof(HouseId))]
    public virtual PlayerHouse House { get; private set; }

    public int Slot { get; private set; }

    /// <summary>
    /// X position of the furniture on the map.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y position of the furniture on the map.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Direction/rotation of the furniture.
    /// </summary>
    public int Direction { get; set; }

    /// <summary>
    /// Additional properties stored as JSON for extensibility.
    /// Can store custom data like color, state, etc.
    /// </summary>
    public string Properties { get; set; } = "{}";
}
