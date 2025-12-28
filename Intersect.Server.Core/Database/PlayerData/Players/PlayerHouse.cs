using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Collections.Slotting;
using Intersect.Core;
using Intersect.Framework.Core;
using Intersect.Server.Entities;
using Intersect.Server.Networking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Server.Database.PlayerData.Players;

/// <summary>
/// Represents a player's house with furniture storage and visitor permissions.
/// </summary>
public partial class PlayerHouse
{
    public static readonly ConcurrentDictionary<Guid, PlayerHouse> Houses = new();

    // Entity Framework constructor
    public PlayerHouse()
    {
    }

    /// <summary>
    /// The database Id of the house.
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the player who owns this house.
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    /// Reference to the owner player.
    /// </summary>
    [ForeignKey(nameof(OwnerId))]
    [JsonIgnore]
    public virtual Player Owner { get; private set; }

    /// <summary>
    /// The map ID that serves as the house template/interior.
    /// </summary>
    public Guid MapId { get; set; }

    /// <summary>
    /// The unique instance ID for this house. Used to create separate map instances.
    /// </summary>
    public Guid HouseInstanceId { get; private set; }

    /// <summary>
    /// The date this house was purchased.
    /// </summary>
    public DateTime PurchaseDate { get; private set; }

    /// <summary>
    /// Furniture slots for this house.
    /// </summary>
    [JsonIgnore]
    public virtual SlotList<HouseFurnitureSlot> Furniture { get; set; } = new SlotList<HouseFurnitureSlot>(
        Options.Instance.Player.MaxHouseFurnitureSlots,
        HouseFurnitureSlot.Create
    );

    /// <summary>
    /// Maximum number of furniture slots for this house.
    /// </summary>
    public int FurnitureSlotsCount { get; set; } = Options.Instance.Player.MaxHouseFurnitureSlots;

    /// <summary>
    /// List of players who have visitor access to this house.
    /// </summary>
    [JsonIgnore]
    public virtual List<HouseVisitor> Visitors { get; set; } = new List<HouseVisitor>();

    /// <summary>
    /// Locking context to prevent concurrent modifications.
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    private object mLock = new object();

    /// <summary>
    /// Getter for the house lock.
    /// </summary>
    public object Lock => mLock;

    /// <summary>
    /// Creates a new house for a player.
    /// </summary>
    /// <param name="owner">The player purchasing the house.</param>
    /// <param name="mapId">The map ID to use as the house interior.</param>
    /// <returns>The created PlayerHouse or null if creation failed.</returns>
    public static PlayerHouse? CreateHouse(Player owner, Guid mapId)
    {
        if (owner == null || mapId == Guid.Empty)
        {
            return null;
        }

        // Check if player already has a house
        if (owner.House != null)
        {
            return null;
        }

        using var context = DbInterface.CreatePlayerContext(readOnly: false);

        owner.Save(context);

        var house = new PlayerHouse
        {
            OwnerId = owner.Id,
            MapId = mapId,
            HouseInstanceId = Guid.NewGuid(),
            PurchaseDate = DateTime.UtcNow,
        };

        context.PlayerHouses.Add(house);

        SlotHelper.ValidateSlotList(house.Furniture, house.FurnitureSlotsCount);

        owner.House = house;

        context.ChangeTracker.DetectChanges();
        context.SaveChanges();

        Houses.AddOrUpdate(house.Id, house, (_, _) => house);

        return house;
    }

    /// <summary>
    /// Loads a house from the database.
    /// </summary>
    /// <param name="id">The house ID to load.</param>
    /// <returns>The loaded house or null if not found.</returns>
    public static PlayerHouse? LoadHouse(Guid id)
    {
        if (Houses.TryGetValue(id, out PlayerHouse? found))
        {
            return found;
        }

        using var context = DbInterface.CreatePlayerContext();
        var house = context.PlayerHouses.Where(h => h.Id == id)
            .Include(h => h.Furniture)
            .Include(h => h.Visitors)
            .AsSplitQuery()
            .FirstOrDefault();

        if (house == default)
        {
            return default;
        }

        SlotHelper.ValidateSlotList(house.Furniture, house.FurnitureSlotsCount);

        Houses.AddOrUpdate(id, house, (_, _) => house);

        return house;
    }

    /// <summary>
    /// Loads a house by owner ID.
    /// </summary>
    /// <param name="ownerId">The player ID who owns the house.</param>
    /// <returns>The loaded house or null if not found.</returns>
    public static PlayerHouse? LoadHouseByOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            return null;
        }

        // Check cache first
        var cached = Houses.Values.FirstOrDefault(h => h.OwnerId == ownerId);
        if (cached != null)
        {
            return cached;
        }

        using var context = DbInterface.CreatePlayerContext();
        var house = context.PlayerHouses.Where(h => h.OwnerId == ownerId)
            .Include(h => h.Furniture)
            .Include(h => h.Visitors)
            .AsSplitQuery()
            .FirstOrDefault();

        if (house == default)
        {
            return default;
        }

        SlotHelper.ValidateSlotList(house.Furniture, house.FurnitureSlotsCount);

        Houses.AddOrUpdate(house.Id, house, (_, _) => house);

        return house;
    }

    /// <summary>
    /// Adds a visitor to this house with specified permissions.
    /// </summary>
    /// <param name="visitorId">The player ID to grant access.</param>
    /// <param name="permission">The permission level to grant.</param>
    /// <returns>True if the visitor was added successfully.</returns>
    public bool AddVisitor(Guid visitorId, HousePermission permission)
    {
        if (visitorId == Guid.Empty || visitorId == OwnerId)
        {
            return false;
        }

        // Check if visitor already exists
        var existing = Visitors.FirstOrDefault(v => v.VisitorId == visitorId);
        if (existing != null)
        {
            existing.Permission = permission;
            Save();
            return true;
        }

        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        var visitor = new HouseVisitor
        {
            HouseId = Id,
            VisitorId = visitorId,
            Permission = permission,
            InvitedDate = DateTime.UtcNow
        };

        Visitors.Add(visitor);
        context.Update(this);
        context.ChangeTracker.DetectChanges();
        context.SaveChanges();

        return true;
    }

    /// <summary>
    /// Removes a visitor from this house.
    /// </summary>
    /// <param name="visitorId">The player ID to remove.</param>
    /// <returns>True if the visitor was removed successfully.</returns>
    public bool RemoveVisitor(Guid visitorId)
    {
        var visitor = Visitors.FirstOrDefault(v => v.VisitorId == visitorId);
        if (visitor == null)
        {
            return false;
        }

        Visitors.Remove(visitor);
        Save();

        return true;
    }

    /// <summary>
    /// Gets the permission level for a specific player.
    /// </summary>
    /// <param name="playerId">The player ID to check.</param>
    /// <returns>The permission level for the player.</returns>
    public HousePermission GetPermission(Guid playerId)
    {
        if (playerId == OwnerId)
        {
            return HousePermission.Owner;
        }

        var visitor = Visitors.FirstOrDefault(v => v.VisitorId == playerId);
        return visitor?.Permission ?? HousePermission.None;
    }

    /// <summary>
    /// Checks if a player can enter this house.
    /// </summary>
    /// <param name="playerId">The player ID to check.</param>
    /// <returns>True if the player can enter.</returns>
    public bool CanEnter(Guid playerId)
    {
        var permission = GetPermission(playerId);
        return permission != HousePermission.None;
    }

    /// <summary>
    /// Checks if a player can modify furniture in this house.
    /// </summary>
    /// <param name="playerId">The player ID to check.</param>
    /// <returns>True if the player can modify furniture.</returns>
    public bool CanModifyFurniture(Guid playerId)
    {
        var permission = GetPermission(playerId);
        return permission == HousePermission.Owner || permission == HousePermission.Modify;
    }

    /// <summary>
    /// Sends furniture slot update to the owner if they are online and in the house.
    /// </summary>
    /// <param name="slot">Slot index to send the update for.</param>
    public void FurnitureSlotUpdated(int slot)
    {
        var owner = Player.FindOnline(OwnerId);
        if (owner?.HouseInterface != null)
        {
            owner.HouseInterface.SendFurnitureUpdate(slot, false);
        }
    }

    /// <summary>
    /// Expands the number of furniture slots for this house.
    /// </summary>
    /// <param name="count">The new slot count.</param>
    public void ExpandFurnitureSlots(int count)
    {
        if (FurnitureSlotsCount >= count || count > Options.Instance.Player.MaxHouseFurnitureSlots)
        {
            return;
        }

        lock (mLock)
        {
            FurnitureSlotsCount = count;
            SlotHelper.ValidateSlotList(Furniture, FurnitureSlotsCount);
            DbInterface.Pool.QueueWorkItem(Save);
        }
    }

    /// <summary>
    /// Saves the house to the database.
    /// </summary>
    public void Save()
    {
        lock (mLock)
        {
            using (var context = DbInterface.CreatePlayerContext(readOnly: false))
            {
                context.Update(this);
                context.ChangeTracker.DetectChanges();
                context.SaveChanges();
            }
        }
    }

    /// <summary>
    /// Deletes a house from the game.
    /// </summary>
    /// <param name="house">The house to delete.</param>
    public static void DeleteHouse(PlayerHouse house)
    {
        if (house == null)
        {
            return;
        }

        var owner = Player.FindOnline(house.OwnerId);
        if (owner != null)
        {
            owner.House = null;

            if (owner.HouseInterface != null)
            {
                owner.HouseInterface.Dispose();
            }
        }

        using var context = DbInterface.CreatePlayerContext(readOnly: false);
        context.PlayerHouses.Remove(house);
        context.ChangeTracker.DetectChanges();
        context.SaveChanges();

        _ = Houses.TryRemove(house.Id, out _);
    }

    /// <summary>
    /// Called when the owner logs out to potentially unload the house from memory.
    /// </summary>
    public void NotifyOwnerDisposed()
    {
        Save();

        if (Houses.TryRemove(Id, out _))
        {
            ApplicationContext.Context.Value?.Logger.LogInformation($"[House][{Id}] Removed from cache after owner logged out");
        }
    }
}

/// <summary>
/// Defines permission levels for house visitors.
/// </summary>
public enum HousePermission
{
    /// <summary>
    /// No access to the house.
    /// </summary>
    None = 0,

    /// <summary>
    /// Can view the house but not modify anything.
    /// </summary>
    View = 1,

    /// <summary>
    /// Can view and modify furniture.
    /// </summary>
    Modify = 2,

    /// <summary>
    /// Owner of the house with full permissions.
    /// </summary>
    Owner = 99
}
