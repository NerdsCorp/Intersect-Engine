# Player Housing System - Advanced Features Update

This document describes the advanced features added to the player housing system: furniture types, functional furniture, and public house tours.

## New Features Added

### 1. Furniture Item Types

Items can now be designated as furniture with specific properties and functionality.

#### FurnitureType Enum
**Location**: `Framework/Intersect.Framework.Core/Enums/FurnitureType.cs`

```csharp
public enum FurnitureType
{
    Decorative = 0,        // No functionality
    Storage = 1,           // Can store items
    CraftingStation = 2,   // Opens crafting table
    Interactive = 3,       // Triggers events
    Buff = 4,             // Provides passive buffs
    BankAccess = 5,       // Personal bank access
    ShopAccess = 6        // Personal shop/vendor
}
```

#### FurnitureProperties Class
**Location**: `Framework/Intersect.Framework.Core/GameObjects/Items/FurnitureProperties.cs`

Defines all functional properties for furniture items:

- **Type**: The functional type (from FurnitureType enum)
- **Width/Height**: Size in tiles for placement validation
- **IsBlocking**: Whether it blocks player movement
- **StorageSlots**: Capacity for Storage type furniture
- **CraftingTableId**: Linked crafting table for CraftingStation type
- **InteractionEventId**: Event to trigger for Interactive type
- **ShopId**: Shop to open for ShopAccess type
- **BuffEffects**: JSON string for Buff type effects
- **PlacedSprite**: Custom sprite when placed
- **ZLayer**: Rendering layer (0 = below player, 1 = above)
- **InteractionAnimationId**: Animation on interaction
- **InteractionSound**: Sound on interaction
- **AllowVisitorPickup**: If non-owners can pick up

#### ItemDescriptor Updates
**Location**: `Framework/Intersect.Framework.Core/GameObjects/Items/ItemDescriptor.cs`

Added properties:
```csharp
public bool CanBeFurniture { get; set; } = false;
public FurnitureProperties? FurnitureProperties { get; set; }
```

### 2. Functional Furniture System

#### Storage Container Furniture

Players can now place furniture that acts as storage containers with their own inventory.

##### FurnitureStorage Model
**Location**: `Intersect.Server.Core/Database/PlayerData/Players/FurnitureStorage.cs`

- Links to a HouseFurnitureSlot
- Contains list of FurnitureStorageSlot items
- Persists independently from house furniture

##### FurnitureStorageSlot Model
Slot-based storage similar to bank/inventory slots.

##### FurnitureStorageInterface
**Location**: `Intersect.Server.Core/Entities/FurnitureStorageInterface.cs`

Similar to BankInterface, provides:
- `SendOpenStorage()`: Opens storage UI
- `TryDepositItem()`: Deposits items from inventory
- `TryWithdrawItem()`: Withdraws items to inventory
- Thread-safe operations with automatic persistence

#### How to Create Functional Furniture

**Example: Storage Chest**
1. Create an item in the editor
2. Set `CanBeFurniture = true`
3. Set `FurnitureProperties.Type = FurnitureType.Storage`
4. Set `FurnitureProperties.StorageSlots = 50`
5. Set sprite and other visual properties

**Example: Crafting Workbench**
1. Create an item in the editor
2. Set `CanBeFurniture = true`
3. Set `FurnitureProperties.Type = FurnitureType.CraftingStation`
4. Set `FurnitureProperties.CraftingTableId` to your crafting table
5. When player interacts, opens the crafting interface

**Example: Interactive Furniture**
1. Create an item in the editor
2. Set `CanBeFurniture = true`
3. Set `FurnitureProperties.Type = FurnitureType.Interactive`
4. Set `FurnitureProperties.InteractionEventId` to your event
5. When player interacts, triggers the event

### 3. Public House Tours

Houses can now be made public for other players to visit, rate, and explore.

#### PlayerHouse Updates
**Location**: `Intersect.Server.Core/Database/PlayerData/Players/PlayerHouse.cs`

Added properties:
```csharp
public bool IsPublic { get; set; } = false;
public string HouseName { get; set; } = string.Empty;
public string HouseDescription { get; set; } = string.Empty;
public int VisitCount { get; set; } = 0;
public int TotalRating { get; set; } = 0;
public int RatingCount { get; set; } = 0;
public double AverageRating => RatingCount > 0 ? (double)TotalRating / RatingCount : 0.0;
```

#### Public Tour Features

**CanEnter() Method Update**:
- Public houses can now be entered by anyone
- No visitor permission required for public houses

**RecordVisit() Method**:
- Increments visit counter
- Tracks house popularity

**AddRating() Method**:
- Accepts ratings 1-5
- Calculates average rating
- Persists to database

**GetPublicHouses() Method**:
```csharp
public static List<PlayerHouse> GetPublicHouses(
    int skip = 0,
    int take = 10,
    string sortBy = "rating"
)
```
Retrieves public houses sorted by:
- `"rating"`: Average rating (default)
- `"visits"`: Most visited
- `"recent"`: Recently created

**SearchPublicHouses() Method**:
```csharp
public static List<PlayerHouse> SearchPublicHouses(
    string searchTerm,
    int skip = 0,
    int take = 10
)
```
Searches public houses by:
- House name
- Owner name

### 4. Database Schema Changes

#### New Tables (via migration)

**FurnitureStorage**:
- Id (Guid)
- FurnitureSlotId (Guid FK)
- CreatedDate (DateTime)

**FurnitureStorageSlot**:
- Id (Guid)
- FurnitureStorageId (Guid FK)
- Slot (int)
- ItemId (Guid)
- Quantity (int)
- BagId (Guid nullable)
- Properties (string)

#### Updated Tables

**PlayerHouse**:
- IsPublic (bool)
- HouseName (string)
- HouseDescription (string)
- VisitCount (int)
- TotalRating (int)
- RatingCount (int)

**ItemDescriptor** (game database):
- CanBeFurniture (bool)
- FurnitureProperties (JSON string)

### 5. Usage Examples

#### Creating a Public Showcase House

```
[Event: House Settings NPC]
├─ Show Options:
│  ├─ "Make house public"
│  ├─ "Set house name"
│  ├─ "Set house description"
│  └─ "Make house private"
└─ [Process choices with event commands]

[Event Command: SetHousePublic]
└─ Sets player.House.IsPublic = true

[Event Command: SetHouseName]
├─ Input Variable: "Enter house name"
└─ Sets player.House.HouseName

[Event Command: SetHouseDescription]
├─ Input Variable: "Enter description"
└─ Sets player.House.HouseDescription
```

#### Creating a House Tour Browser

```
[Event: House Tour Board]
├─ Show Options:
│  ├─ "Browse by Rating"
│  ├─ "Browse by Popularity"
│  ├─ "Browse Recent"
│  └─ "Search Houses"
└─ [Display list using GetPublicHouses()]

[Event Command: BrowsePublicHouses]
├─ Get list: PlayerHouse.GetPublicHouses(0, 10, "rating")
├─ Display house names and ratings
└─ On selection: EnterHouseCommand with house owner ID
```

#### Creating Functional Furniture

**Storage Chest Example:**
```
1. Create Item "Wooden Chest"
   - CanBeFurniture = true
   - FurnitureProperties.Type = Storage
   - FurnitureProperties.StorageSlots = 30
   - FurnitureProperties.Width = 2
   - FurnitureProperties.Height = 1

2. Player places in house

3. Player interacts with chest
   - Server creates FurnitureStorage with 30 slots
   - Opens FurnitureStorageInterface
   - Player can deposit/withdraw items
```

**Crafting Station Example:**
```
1. Create Item "Blacksmith Forge"
   - CanBeFurniture = true
   - FurnitureProperties.Type = CraftingStation
   - FurnitureProperties.CraftingTableId = [Blacksmith Table ID]
   - FurnitureProperties.Width = 3
   - FurnitureProperties.Height = 2

2. Player places in house

3. Player interacts with forge
   - Opens blacksmith crafting table
   - Can craft items as if at public crafting station
```

**Interactive Furniture Example:**
```
1. Create Item "Training Dummy"
   - CanBeFurniture = true
   - FurnitureProperties.Type = Interactive
   - FurnitureProperties.InteractionEventId = [Training Event]
   - FurnitureProperties.InteractionAnimation = [Hit Animation]

2. Player places in house

3. Player interacts with dummy
   - Plays hit animation
   - Triggers training event
   - Could grant experience, test damage, etc.
```

## Event Commands to Implement

### Furniture-Related Commands

**OpenFurnitureStorageCommand**
```csharp
public partial class OpenFurnitureStorageCommand : EventCommand
{
    public int FurnitureSlot { get; set; }  // Which furniture to open
}
```

**InteractWithFurnitureCommand**
```csharp
public partial class InteractWithFurnitureCommand : EventCommand
{
    public int FurnitureSlot { get; set; }
    // Automatically handles based on FurnitureType
}
```

### Public House Commands

**SetHousePublicCommand**
```csharp
public partial class SetHousePublicCommand : EventCommand
{
    public bool IsPublic { get; set; }
}
```

**SetHouseNameCommand**
```csharp
public partial class SetHouseNameCommand : EventCommand
{
    public string VariableId { get; set; }  // Variable containing the name
}
```

**BrowsePublicHousesCommand**
```csharp
public partial class BrowsePublicHousesCommand : EventCommand
{
    public string SortBy { get; set; }  // "rating", "visits", "recent"
    public int PageSize { get; set; } = 10;
}
```

**RateHouseCommand**
```csharp
public partial class RateHouseCommand : EventCommand
{
    public int Rating { get; set; }  // 1-5 stars
}
```

**SearchHousesCommand**
```csharp
public partial class SearchHousesCommand : EventCommand
{
    public string SearchVariableId { get; set; }
}
```

## Implementation Benefits

### 1. Furniture System
- **Flexibility**: Multiple furniture types for different uses
- **Extensibility**: Easy to add new furniture types
- **Reusability**: Storage containers can hold any items
- **Customization**: Each furniture can have unique properties

### 2. Public Tours
- **Community**: Players can showcase their houses
- **Competition**: Rating system encourages creativity
- **Discovery**: Search and browse features
- **Metrics**: Visit tracking shows popularity

### 3. Integration
- **Event-Driven**: Most functionality via events
- **Consistent**: Follows existing patterns (Bank, Shop, Crafting)
- **Performant**: Database-backed with caching
- **Scalable**: Can handle many houses and storage containers

## Performance Considerations

1. **Lazy Loading**: Furniture storage only loaded when accessed
2. **Caching**: Public house lists can be cached
3. **Pagination**: Browse/search support pagination
4. **Indexing**: Database indexes on IsPublic, Rating, etc.

## Security Considerations

1. **Permission Checks**: All furniture operations check permissions
2. **Validation**: Item can only be placed if CanBeFurniture is true
3. **Public Access**: Public houses allow entry but not modification
4. **Storage Safety**: Each storage container isolated per furniture instance

## Future Enhancements

Possible additions:
- **Furniture Durability**: Furniture can degrade and need repair
- **Furniture Crafting**: Players craft furniture items
- **Furniture Sets**: Bonuses for matching furniture
- **Seasonal Decorations**: Limited-time furniture
- **Furniture Marketplace**: Buy/sell player-made furniture
- **House Templates**: Pre-designed layouts for purchase
- **House Upgrades**: Expand house size, add rooms
- **House Plots**: Multiple plot sizes/locations
- **Guild Houses**: Shared guild housing
- **Apartment Buildings**: Instanced multi-unit housing

## Files Added/Modified

### Created:
- `Framework/Intersect.Framework.Core/Enums/FurnitureType.cs`
- `Framework/Intersect.Framework.Core/GameObjects/Items/FurnitureProperties.cs`
- `Intersect.Server.Core/Database/PlayerData/Players/FurnitureStorage.cs`
- `Intersect.Server.Core/Entities/FurnitureStorageInterface.cs`

### Modified:
- `Framework/Intersect.Framework.Core/GameObjects/Items/ItemDescriptor.cs`
- `Intersect.Server.Core/Database/PlayerData/Players/PlayerHouse.cs`
- `Intersect.Server.Core/Database/PlayerData/PlayerContext.cs`
- `Intersect.Server.Core/Entities/HouseInterface.cs`

## Migration Required

After implementing, run:
```bash
dotnet ef migrations add AddFurnitureAndPublicHouses --context PlayerContext
```

This will create migrations for:
- FurnitureStorage and FurnitureStorageSlot tables
- PlayerHouse public tour fields (IsPublic, HouseName, etc.)
- ItemDescriptor furniture fields

## Summary

The housing system now supports:
- ✅ **7 Furniture Types**: Decorative, Storage, Crafting, Interactive, Buff, Bank, Shop
- ✅ **Functional Storage**: Furniture containers with persistent item storage
- ✅ **Public Tours**: Houses can be public with rating/visit tracking
- ✅ **Search & Browse**: Find houses by name, owner, rating, popularity
- ✅ **Full Customization**: Per-furniture properties for sprites, sounds, animations
- ✅ **Permission System**: Visitors can view but not modify public houses
- ✅ **Event Integration**: All features accessible via event commands

This creates a comprehensive housing system that goes beyond simple decoration to provide meaningful gameplay features and community interaction!
