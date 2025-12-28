namespace Intersect.Enums;

/// <summary>
/// Defines the functional type of furniture items.
/// </summary>
public enum FurnitureType
{
    /// <summary>
    /// Decorative furniture with no functionality.
    /// </summary>
    Decorative = 0,

    /// <summary>
    /// Storage container that can hold items.
    /// </summary>
    Storage = 1,

    /// <summary>
    /// Crafting station that opens a crafting table.
    /// </summary>
    CraftingStation = 2,

    /// <summary>
    /// Furniture that triggers an event when interacted with.
    /// </summary>
    Interactive = 3,

    /// <summary>
    /// Furniture that provides passive buffs to house owner.
    /// </summary>
    Buff = 4,

    /// <summary>
    /// Personal bank access point.
    /// </summary>
    BankAccess = 5,

    /// <summary>
    /// Personal shop/vendor.
    /// </summary>
    ShopAccess = 6
}
