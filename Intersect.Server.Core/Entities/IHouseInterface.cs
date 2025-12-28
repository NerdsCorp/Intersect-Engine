namespace Intersect.Server.Entities;

/// <summary>
/// Interface for managing player house furniture.
/// </summary>
public interface IHouseInterface : IDisposable
{
    /// <summary>
    /// Sends the initial house furniture data to the player.
    /// </summary>
    void SendOpenHouse();

    /// <summary>
    /// Sends an update for a specific furniture slot.
    /// </summary>
    /// <param name="slot">The furniture slot index.</param>
    /// <param name="sendToAll">Whether to send to all players in the house.</param>
    void SendFurnitureUpdate(int slot, bool sendToAll = true);

    /// <summary>
    /// Closes the house interface for the player.
    /// </summary>
    void SendCloseHouse();

    /// <summary>
    /// Places a furniture item from inventory into the house.
    /// </summary>
    /// <param name="inventorySlotIndex">The inventory slot containing the furniture.</param>
    /// <param name="x">X position on the map.</param>
    /// <param name="y">Y position on the map.</param>
    /// <param name="direction">Direction/rotation of the furniture.</param>
    /// <returns>True if the furniture was placed successfully.</returns>
    bool TryPlaceFurniture(int inventorySlotIndex, int x, int y, int direction = 0);

    /// <summary>
    /// Removes furniture from the house and returns it to inventory.
    /// </summary>
    /// <param name="furnitureSlotIndex">The furniture slot to remove.</param>
    /// <returns>True if the furniture was removed successfully.</returns>
    bool TryRemoveFurniture(int furnitureSlotIndex);

    /// <summary>
    /// Moves furniture to a new position.
    /// </summary>
    /// <param name="furnitureSlotIndex">The furniture slot to move.</param>
    /// <param name="x">New X position.</param>
    /// <param name="y">New Y position.</param>
    /// <param name="direction">New direction/rotation.</param>
    /// <returns>True if the furniture was moved successfully.</returns>
    bool TryMoveFurniture(int furnitureSlotIndex, int x, int y, int direction = 0);
}
