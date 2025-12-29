using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.GameObjects;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Items;

/// <summary>
/// Defines functional properties for furniture items.
/// </summary>
public partial class FurnitureProperties
{
    /// <summary>
    /// The functional type of this furniture.
    /// </summary>
    public FurnitureType Type { get; set; } = FurnitureType.Decorative;

    /// <summary>
    /// Width of the furniture in tiles (for placement validation).
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// Height of the furniture in tiles (for placement validation).
    /// </summary>
    public int Height { get; set; } = 1;

    /// <summary>
    /// Whether this furniture blocks player movement.
    /// </summary>
    public bool IsBlocking { get; set; } = true;

    /// <summary>
    /// Storage capacity for Storage type furniture.
    /// </summary>
    public int StorageSlots { get; set; } = 0;

    /// <summary>
    /// Crafting table ID for CraftingStation type furniture.
    /// </summary>
    [Column("CraftingTable")]
    [JsonProperty]
    public Guid CraftingTableId { get; set; }

    /// <summary>
    /// Crafting table descriptor for CraftingStation type furniture.
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public CraftingTableDescriptor CraftingTable
    {
        get => CraftingTableDescriptor.Get(CraftingTableId);
        set => CraftingTableId = value?.Id ?? Guid.Empty;
    }

    /// <summary>
    /// Event ID for Interactive type furniture.
    /// </summary>
    [Column("InteractionEvent")]
    [JsonProperty]
    public Guid InteractionEventId { get; set; }

    /// <summary>
    /// Event descriptor for Interactive type furniture.
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public EventDescriptor InteractionEvent
    {
        get => EventDescriptor.Get(InteractionEventId);
        set => InteractionEventId = value?.Id ?? Guid.Empty;
    }

    /// <summary>
    /// Shop ID for ShopAccess type furniture.
    /// </summary>
    [Column("Shop")]
    [JsonProperty]
    public Guid ShopId { get; set; }

    /// <summary>
    /// Shop descriptor for ShopAccess type furniture.
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public ShopDescriptor Shop
    {
        get => ShopDescriptor.Get(ShopId);
        set => ShopId = value?.Id ?? Guid.Empty;
    }

    /// <summary>
    /// Buff effects for Buff type furniture (stored as JSON).
    /// </summary>
    public string BuffEffects { get; set; } = "{}";

    /// <summary>
    /// Custom sprite override for furniture when placed.
    /// </summary>
    public string PlacedSprite { get; set; } = string.Empty;

    /// <summary>
    /// Z-layer for rendering (0 = below player, 1 = above player).
    /// </summary>
    public int ZLayer { get; set; } = 0;

    /// <summary>
    /// Animation ID to play when furniture is interacted with.
    /// </summary>
    [Column("InteractionAnimation")]
    [JsonProperty]
    public Guid InteractionAnimationId { get; set; }

    /// <summary>
    /// Sound to play when furniture is interacted with.
    /// </summary>
    public string InteractionSound { get; set; } = string.Empty;

    /// <summary>
    /// Whether this furniture can be picked up by non-owners with Modify permission.
    /// </summary>
    public bool AllowVisitorPickup { get; set; } = false;
}
