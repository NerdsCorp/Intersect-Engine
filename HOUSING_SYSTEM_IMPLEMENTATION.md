# Player Housing System Implementation Guide

## Overview

This document describes the implementation of a comprehensive player housing system for Intersect Engine. The system follows the existing Guild pattern and integrates seamlessly with Intersect's event system and map instancing architecture.

## Design Philosophy

The housing system uses a **Hybrid Event + Source Modification** approach:

- **Source Modifications**: Handle core functionality (map instancing, database storage, furniture management)
- **Event Commands**: Handle game logic (house purchasing, door interactions, decorations)

This design leverages Intersect's powerful event system while adding the necessary infrastructure in the engine source.

## Completed Implementation

### 1. Database Models

#### PlayerHouse.cs
**Location**: `Intersect.Server.Core/Database/PlayerData/Players/PlayerHouse.cs`

Key features:
- One house per player (1:1 relationship with Player)
- Unique `HouseInstanceId` for personal map instances
- `SlotList<HouseFurnitureSlot>` for furniture storage
- `List<HouseVisitor>` for visitor permissions
- Thread-safe with locking mechanism
- Follows Guild pattern for persistence and caching

Properties:
- `Id`: Unique house identifier
- `OwnerId`: Foreign key to Player
- `MapId`: Template map for house interior
- `HouseInstanceId`: Unique instance ID for map separation
- `PurchaseDate`: When the house was acquired
- `FurnitureSlotsCount`: Maximum furniture slots (configurable)
- `Furniture`: Slot-based furniture storage
- `Visitors`: List of players with access

Methods:
- `CreateHouse()`: Creates a new house for a player
- `LoadHouse()` / `LoadHouseByOwner()`: Loads from database
- `AddVisitor()` / `RemoveVisitor()`: Visitor management
- `GetPermission()` / `CanEnter()` / `CanModifyFurniture()`: Permission checks
- `ExpandFurnitureSlots()`: Expand furniture capacity
- `Save()`: Persist to database

#### HouseFurnitureSlot.cs
**Location**: `Intersect.Server.Core/Database/PlayerData/Players/HouseFurnitureSlot.cs`

Extends the base `Item` class with position data:
- `Slot`: Slot index
- `X`, `Y`: Position on the map
- `Direction`: Rotation/orientation
- `Properties`: JSON string for extended data
- Supports bags for container furniture

#### HouseVisitor.cs
**Location**: `Intersect.Server.Core/Database/PlayerData/Players/HouseVisitor.cs`

Manages visitor permissions:
- `VisitorId`: Player who has access
- `Permission`: Level of access (None, View, Modify, Owner)
- `InvitedDate`: When access was granted

### 2. House Permission System

```csharp
public enum HousePermission
{
    None = 0,      // No access
    View = 1,      // Can enter and view
    Modify = 2,    // Can place/remove furniture
    Owner = 99     // Full control
}
```

### 3. Map Modifications

#### MapDescriptor.cs
**Location**: `Framework/Intersect.Framework.Core/GameObjects/Maps/MapDescriptor.cs`

Added property:
```csharp
public bool IsPersonalInstanceMap { get; set; }
```

This flag indicates that a map can be instanced per player for housing. When designing house interiors in the editor, set this flag to true.

### 4. Player Modifications

#### Player.cs
**Location**: `Intersect.Server.Core/Entities/Player.cs`

Added properties:
```csharp
// House ownership
public virtual PlayerHouse House { get; set; }

// Currently visiting house (may not be player's own)
[NotMapped, JsonIgnore]
public Guid VisitingHouseId { get; set; } = Guid.Empty;

// House interface for furniture management
[NotMapped, JsonIgnore]
public IHouseInterface HouseInterface { get; set; }

// Quick check if player is in house interface
[NotMapped]
public bool InHouse => HouseInterface != null;
```

### 5. Database Context

#### PlayerContext.cs
**Location**: `Intersect.Server.Core/Database/PlayerData/PlayerContext.cs`

Added DbSets:
```csharp
public DbSet<PlayerHouse> PlayerHouses { get; set; }
public DbSet<HouseFurnitureSlot> House_Furniture { get; set; }
public DbSet<HouseVisitor> House_Visitors { get; set; }
```

Configured relationships:
```csharp
// Player has one house
modelBuilder.Entity<Player>()
    .HasOne(p => p.House)
    .WithOne(h => h.Owner)
    .HasForeignKey<PlayerHouse>(h => h.OwnerId)
    .OnDelete(DeleteBehavior.Cascade);

// House has many furniture slots
modelBuilder.Entity<PlayerHouse>()
    .HasMany(h => h.Furniture)
    .WithOne(f => f.House)
    .OnDelete(DeleteBehavior.Cascade);

// House has many visitors
modelBuilder.Entity<PlayerHouse>()
    .HasMany(h => h.Visitors)
    .WithOne(v => v.House)
    .OnDelete(DeleteBehavior.Cascade);

// Furniture can have bags
modelBuilder.Entity<HouseFurnitureSlot>().HasOne(f => f.Bag);
```

### 6. Configuration

#### PlayerOptions.cs
**Location**: `Framework/Intersect.Framework.Core/Config/PlayerOptions.cs`

Added settings:
```csharp
public int MaxHouseFurnitureSlots { get; set; } = 100;
public int MaxHouseVisitors { get; set; } = 50;
```

### 7. House Interface

#### IHouseInterface.cs
**Location**: `Intersect.Server.Core/Entities/IHouseInterface.cs`

Interface for furniture management:
- `SendOpenHouse()`: Send initial furniture data
- `SendFurnitureUpdate()`: Update specific furniture slot
- `SendCloseHouse()`: Close house interface
- `TryPlaceFurniture()`: Place furniture from inventory
- `TryRemoveFurniture()`: Remove furniture to inventory
- `TryMoveFurniture()`: Reposition furniture

#### HouseInterface.cs
**Location**: `Intersect.Server.Core/Entities/HouseInterface.cs`

Implementation with:
- Permission checking before modifications
- Thread-safe operations with locking
- Automatic database persistence
- Packet sending for client updates

## TODO: Remaining Implementation

### 1. Event Commands

The following event commands need to be implemented in `Framework/Intersect.Framework.Core/GameObjects/Events/Commands/`:

#### PurchaseHouseCommand.cs
```csharp
public partial class PurchaseHouseCommand : EventCommand
{
    public Guid MapId { get; set; }  // Map to use as house interior
    public int Cost { get; set; }    // Cost in currency
    public Guid CurrencyId { get; set; }
}
```

#### EnterHouseCommand.cs
```csharp
public partial class EnterHouseCommand : EventCommand
{
    public Guid? TargetPlayerId { get; set; }  // null = own house, or specify player ID
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }
}
```

#### OpenHouseFurnitureCommand.cs
```csharp
public partial class OpenHouseFurnitureCommand : EventCommand
{
    // Opens the furniture management interface
}
```

#### InviteToHouseCommand.cs
```csharp
public partial class InviteToHouseCommand : EventCommand
{
    public string TargetPlayerName { get; set; }
    public HousePermission Permission { get; set; }
}
```

#### RemoveVisitorCommand.cs
```csharp
public partial class RemoveVisitorCommand : EventCommand
{
    public string TargetPlayerName { get; set; }
}
```

### 2. EventCommandType Enum

Add to `Framework/Intersect.Framework.Core/GameObjects/Events/EventCommandType.cs`:

```csharp
PurchaseHouse,
EnterHouse,
OpenHouseFurniture,
InviteToHouse,
RemoveVisitor,
```

### 3. Command Processing

Add processing methods in `Intersect.Server.Core/Entities/Events/CommandProcessing.cs`:

```csharp
private static void ProcessCommand(
    PurchaseHouseCommand command,
    Player player,
    Event instance,
    CommandInstance stackInfo,
    Stack<CommandInstance> callStack)
{
    // Check if player already has a house
    // Check if player has enough currency
    // Create house with PlayerHouse.CreateHouse()
    // Deduct currency
    // Send success message
}

private static void ProcessCommand(
    EnterHouseCommand command,
    Player player,
    Event instance,
    CommandInstance stackInfo,
    Stack<CommandInstance> callStack)
{
    // Determine which house to enter (own or specified player)
    // Check permissions
    // Warp player to house instance
    // Set VisitingHouseId
}

private static void ProcessCommand(
    OpenHouseFurnitureCommand command,
    Player player,
    Event instance,
    CommandInstance stackInfo,
    Stack<CommandInstance> callStack)
{
    // Get the house player is currently visiting
    // Check permissions
    // Create HouseInterface
    // Send furniture data
}

// Implement similar processors for InviteToHouse and RemoveVisitor
```

### 4. Network Packets

Implement packet classes in `Framework/Intersect.Framework.Core/Network/Packets/`:

#### Server/HousePacket.cs
```csharp
public partial class HousePacket : IPacket
{
    public bool Close { get; set; }
    public Guid HouseId { get; set; }
    public Guid OwnerId { get; set; }
    public int FurnitureSlots { get; set; }
    public HouseFurnitureUpdatePacket[] Furniture { get; set; }
}
```

#### Server/HouseFurnitureUpdatePacket.cs
```csharp
public partial class HouseFurnitureUpdatePacket : IPacket
{
    public int Slot { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Direction { get; set; }
    public string Properties { get; set; }
}
```

#### Client/HouseFurnitureActionPacket.cs
```csharp
public partial class HouseFurnitureActionPacket : IPacket
{
    public enum ActionType
    {
        Place,
        Remove,
        Move
    }

    public ActionType Action { get; set; }
    public int InventorySlot { get; set; }  // For Place
    public int FurnitureSlot { get; set; }  // For Remove/Move
    public int X { get; set; }
    public int Y { get; set; }
    public int Direction { get; set; }
}
```

### 5. Warp Logic for House Instances

Modify the warp logic in `Player.cs` or warp handling code:

```csharp
public void WarpToHouse(Guid houseId)
{
    var house = PlayerHouse.LoadHouse(houseId);
    if (house == null || !house.CanEnter(Id))
    {
        return;
    }

    // Store current position as overworld position if not in instance
    if (InstanceType == MapInstanceType.Overworld)
    {
        LastOverworldMapId = MapId;
        LastOverworldX = X;
        LastOverworldY = Y;
    }

    // Set instance type and ID
    InstanceType = MapInstanceType.Personal;
    PersonalMapInstanceId = house.HouseInstanceId;
    VisitingHouseId = houseId;

    // Warp to house map
    Warp(house.MapId, spawnX, spawnY);
}

public void ExitHouse()
{
    if (VisitingHouseId == Guid.Empty)
    {
        return;
    }

    VisitingHouseId = Guid.Empty;
    PersonalMapInstanceId = Guid.Empty;
    InstanceType = MapInstanceType.Overworld;

    // Warp back to overworld position
    Warp(LastOverworldMapId, LastOverworldX, LastOverworldY);
}
```

### 6. Database Migration

Create a migration in `Intersect.Server.Core/Migrations/Sqlite/Player/`:

```bash
# In terminal:
cd Intersect.Server.Core
dotnet ef migrations add AddPlayerHousingSystem --context PlayerContext
```

This will generate migration files for both SQLite and MySQL that create the necessary database tables:
- `PlayerHouses`
- `House_Furniture`
- `House_Visitors`

### 7. Editor UI

Add UI elements in `Intersect.Editor/Forms/Editors/frmMap.cs` or map properties:

- Checkbox for "Personal Instance Map" (bound to `IsPersonalInstanceMap`)
- Info tooltip explaining the feature

Location: Map Properties panel, near other map settings like "Is Indoors"

### 8. Client Implementation

The client will need:

1. **House UI**: Similar to bank UI for furniture management
   - Grid view of furniture slots
   - Drag and drop from inventory
   - Visual representation on map

2. **Packet Handlers**:
   - `HousePacket` handler to open/close house interface
   - `HouseFurnitureUpdatePacket` handler to update furniture display

3. **Map Rendering**: Display furniture sprites at their X/Y positions

## Usage Example

### Creating a House System with Events

1. **Create a house interior map** in the editor:
   - Design the interior layout
   - Check "Personal Instance Map" in map properties
   - Add furniture placement zones

2. **Create a house purchase NPC**:
   - Add event with condition: Check if player has house (conditional branch)
   - If no house: Show purchase dialog
   - Use `PurchaseHouseCommand` with map ID and cost
   - Show success message

3. **Create house door event**:
   - When player interacts with their house door
   - Use `EnterHouseCommand` to warp into house instance
   - Optionally specify spawn coordinates

4. **Create furniture management NPC** inside house:
   - Use `OpenHouseFurnitureCommand` to open furniture interface
   - Player can drag items from inventory to place furniture

5. **Create visitor system**:
   - Add event to invite friends: `InviteToHouseCommand`
   - Add event to remove visitors: `RemoveVisitorCommand`
   - Visitors can enter using `EnterHouseCommand` with owner's player ID

### Example Event Flow

```
[Event: House Salesman]
├─ Conditional Branch: Player has house
│  ├─ True:
│  │  └─ Show Text: "You already own a house!"
│  └─ False:
│     ├─ Show Options:
│     │  ├─ "Purchase House (1000 gold)"
│     │  └─ "No thanks"
│     └─ [On Option 1 selected]
│        ├─ Conditional Branch: Player has 1000 gold
│        │  ├─ True:
│        │  │  ├─ Purchase House Command (MapId, Cost)
│        │  │  └─ Show Text: "Welcome to your new home!"
│        │  └─ False:
│        │     └─ Show Text: "You need 1000 gold!"

[Event: House Door]
├─ Conditional Branch: Player has house
│  ├─ True:
│  │  └─ Enter House Command (Own house, spawn at 5,5)
│  └─ False:
│     └─ Show Text: "This door is locked."

[Event: Furniture Manager Inside House]
├─ Show Text: "Would you like to manage your furniture?"
├─ Show Options:
│  ├─ "Open Furniture Interface"
│  ├─ "Invite Friend"
│  └─ "Exit"
└─ [On Option 1]
   └─ Open House Furniture Command
```

## Architecture Benefits

1. **Follows Existing Patterns**: Uses Guild system as template
2. **Event-Driven**: Most gameplay logic in events, not hardcoded
3. **Flexible**: Can create different house types, sizes, costs
4. **Extensible**: Easy to add new features (shared houses, house upgrades)
5. **Performant**: Leverages existing instance system
6. **Multiplayer-Ready**: Visitor system built-in

## Future Enhancements

Possible additions:
- Multiple houses per player
- House upgrades (expand furniture slots)
- Shared houses (roommates)
- House decorations (wallpaper, flooring)
- Functional furniture (crafting stations, storage containers)
- House buffs/bonuses
- House ranking/voting system
- Public house tours

## Testing Checklist

- [ ] Player can purchase a house
- [ ] Player can enter their house
- [ ] House creates a unique instance
- [ ] Player can place furniture from inventory
- [ ] Player can remove furniture to inventory
- [ ] Player can move furniture
- [ ] Visitor can be invited
- [ ] Visitor can enter with View permission
- [ ] Visitor cannot modify with View permission
- [ ] Visitor can modify with Modify permission
- [ ] Owner can remove visitors
- [ ] House data persists across server restarts
- [ ] Multiple players can have different houses
- [ ] Furniture positions save correctly
- [ ] Permissions work correctly for all levels

## Files Modified/Created

### Created:
- `Intersect.Server.Core/Database/PlayerData/Players/PlayerHouse.cs`
- `Intersect.Server.Core/Database/PlayerData/Players/HouseFurnitureSlot.cs`
- `Intersect.Server.Core/Database/PlayerData/Players/HouseVisitor.cs`
- `Intersect.Server.Core/Entities/IHouseInterface.cs`
- `Intersect.Server.Core/Entities/HouseInterface.cs`

### Modified:
- `Framework/Intersect.Framework.Core/Config/PlayerOptions.cs`
- `Framework/Intersect.Framework.Core/GameObjects/Maps/MapDescriptor.cs`
- `Intersect.Server.Core/Database/PlayerData/PlayerContext.cs`
- `Intersect.Server.Core/Entities/Player.cs`

### To Be Created:
- Event command classes (6 files)
- Packet classes (3 files)
- Migration files (2 files - SQLite and MySQL)
- Editor UI modifications
- Client UI components
- Command processing implementations

## Summary

This housing system provides a solid foundation for player housing in Intersect Engine. The core infrastructure is complete, with database models, interfaces, and configuration in place. The remaining work involves implementing the event commands, packets, migrations, and UI components to make the system fully functional.

The hybrid approach allows game developers to create rich housing experiences using events while benefiting from performant, database-backed persistence. The visitor system enables social features, and the furniture system provides creative expression.
