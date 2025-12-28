# Player Housing System - Remaining Client Implementation Tasks

This document outlines the remaining client-side implementation tasks for the player housing system.

## Server-Side Implementation ✅ COMPLETED

The following server-side components have been fully implemented:

1. ✅ Database models (PlayerHouse, HouseFurnitureSlot, HouseVisitor, FurnitureStorage, FurnitureStorageSlot)
2. ✅ Event command classes (10 commands)
3. ✅ Command processing methods (10 processors in CommandProcessing.cs)
4. ✅ Network packets (7 packet classes)
5. ✅ Server packet handlers (2 handlers in PacketHandler.cs)
6. ✅ Localization strings (HousesNamespace, FurnitureStorageNamespace)
7. ✅ Database migrations (SQLite migration created)
8. ✅ Core interfaces (IHouseInterface, HouseInterface, FurnitureStorageInterface)
9. ✅ Configuration (PlayerOptions with MaxHouseFurnitureSlots, MaxHouseVisitors)

## Remaining Client-Side Tasks

### 1. Client Packet Handlers

**Location:** `Intersect.Client.Core/Networking/PacketHandler.cs`

Add handlers for the following server packets:

```csharp
// Handle HousePacket
public void HandlePacket(HousePacket packet)
{
    if (packet.Close)
    {
        Interface.GameUi?.HouseWindow?.Close();
        return;
    }

    // Open/update house window with furniture data
    Interface.GameUi?.HouseWindow?.Update(packet);
}

// Handle HouseFurnitureUpdatePacket
public void HandlePacket(HouseFurnitureUpdatePacket packet)
{
    Interface.GameUi?.HouseWindow?.UpdateFurnitureSlot(packet);
}

// Handle FurnitureStoragePacket
public void HandlePacket(FurnitureStoragePacket packet)
{
    if (packet.Close)
    {
        Interface.GameUi?.FurnitureStorageWindow?.Close();
        return;
    }

    // Open/update furniture storage window
    Interface.GameUi?.FurnitureStorageWindow?.Update(packet);
}

// Handle FurnitureStorageUpdatePacket
public void HandlePacket(FurnitureStorageUpdatePacket packet)
{
    Interface.GameUi?.FurnitureStorageWindow?.UpdateStorageSlot(packet);
}

// Handle PublicHouseListPacket
public void HandlePacket(PublicHouseListPacket packet)
{
    Interface.GameUi?.PublicHouseBrowserWindow?.UpdateList(packet);
}
```

### 2. Client UI Components

**Location:** `Intersect.Client/Interface/Game/`

#### A. HouseWindow.cs

Create a new window for managing house furniture:

**Features:**
- Display furniture slots grid
- Drag & drop furniture from inventory
- Move furniture within the house
- Remove furniture back to inventory
- Visual representation of placed furniture
- Permission checking (owner vs visitor)

**Key Methods:**
```csharp
public void Update(HousePacket packet)
public void UpdateFurnitureSlot(HouseFurnitureUpdatePacket packet)
public void PlaceFurniture(int inventorySlot, int x, int y, int direction)
public void RemoveFurniture(int furnitureSlot)
public void MoveFurniture(int furnitureSlot, int x, int y, int direction)
```

#### B. FurnitureStorageWindow.cs

Create a window for storage furniture containers:

**Features:**
- Display storage slots (similar to bank interface)
- Deposit items from inventory
- Withdraw items to inventory
- Stack splitting support
- Visual feedback for successful operations

**Key Methods:**
```csharp
public void Update(FurnitureStoragePacket packet)
public void UpdateStorageSlot(FurnitureStorageUpdatePacket packet)
public void DepositItem(int inventorySlot, int quantity, int storageSlot = -1)
public void WithdrawItem(int storageSlot, int quantity)
```

#### C. PublicHouseBrowserWindow.cs

Create a window for browsing and searching public houses:

**Features:**
- List of public houses with sorting (rating, visits, name)
- Search functionality
- Display house info (name, owner, rating, description)
- Visit button to enter a public house
- Rating system (1-5 stars)
- Pagination for large lists

**Key Methods:**
```csharp
public void UpdateList(PublicHouseListPacket packet)
public void SearchHouses(string searchTerm)
public void SortHouses(string sortBy)
public void VisitHouse(Guid houseId)
public void RateHouse(Guid houseId, int rating)
```

### 3. Editor UI Components

**Location:** `Intersect.Editor/Forms/Editors/`

#### A. Map Properties - Personal Instance Flag

**File:** `FrmMapProperties.cs` or equivalent

Add a checkbox for "Personal Instance Map" property:

```csharp
// In the map properties form
private CheckBox chkPersonalInstance;

// In the initialization
chkPersonalInstance = new CheckBox
{
    Text = "Personal Instance Map (For Player Housing)",
    AutoSize = true,
    Checked = mMapDescriptor.IsPersonalInstanceMap
};

// In the save method
mMapDescriptor.IsPersonalInstanceMap = chkPersonalInstance.Checked;
```

#### B. Item Editor - Furniture Properties

**File:** `FrmItem.cs`

Add a furniture properties panel:

**UI Elements:**
- Checkbox: "Can Be Furniture"
- ComboBox: Furniture Type (Decorative, Storage, CraftingStation, etc.)
- NumericUpDown: Width, Height, ZLayer
- Checkbox: Is Blocking
- TextBox: Placed Sprite
- NumericUpDown: Storage Slots (for Storage type)
- ComboBox: Crafting Table ID (for CraftingStation type)
- ComboBox: Shop ID (for ShopAccess type)
- ComboBox: Interaction Event ID (for Interactive type)
- TextBox: Buff Effects (for Buff type)

#### C. Event Command Editors

**Location:** `Intersect.Editor/Forms/Editors/Events/Event_CommandList.cs`

Create editor forms for each of the 10 housing commands:

1. **FrmPurchaseHouse.cs**
   - Map selector dropdown
   - Currency selector dropdown
   - Cost numeric input
   - Success/Failure branch selectors

2. **FrmEnterHouse.cs**
   - Radio buttons: Own House / Specific Player / Variable
   - Player ID or Variable selector
   - X, Y spawn position inputs

3. **FrmOpenHouseFurniture.cs**
   - Simple command (no parameters needed)

4. **FrmInviteToHouse.cs**
   - Player Variable selector
   - Permission level dropdown (View, Modify)

5. **FrmRemoveHouseVisitor.cs**
   - Player Variable selector

6. **FrmSetHousePublic.cs**
   - Checkbox: Is Public

7. **FrmSetHouseName.cs**
   - Variable selector for house name

8. **FrmSetHouseDescription.cs**
   - Variable selector for house description

9. **FrmRateHouse.cs**
   - Variable selector for rating (1-5)

10. **FrmOpenFurnitureStorage.cs**
    - Variable selector for furniture slot index

### 4. Testing Checklist

Once all components are implemented, test the following:

#### Basic House Ownership
- [ ] Purchase a house using the event command
- [ ] Enter your own house
- [ ] Verify personal map instance is created

#### Furniture System
- [ ] Place furniture items in the house
- [ ] Move furniture to different positions
- [ ] Rotate furniture (if supported)
- [ ] Remove furniture back to inventory
- [ ] Verify furniture persists after logout/login

#### Visitor System
- [ ] Invite another player to your house
- [ ] Verify visitor can enter with View permission
- [ ] Verify visitor cannot modify furniture with View permission
- [ ] Grant Modify permission
- [ ] Verify visitor can now place/remove furniture
- [ ] Remove visitor access
- [ ] Verify visitor can no longer enter

#### Storage Furniture
- [ ] Place storage furniture
- [ ] Deposit items into storage
- [ ] Withdraw items from storage
- [ ] Verify items persist after closing storage
- [ ] Verify storage capacity limits

#### Functional Furniture
- [ ] Test crafting station furniture
- [ ] Test bank access furniture
- [ ] Test shop access furniture
- [ ] Test buff furniture
- [ ] Test interactive furniture with events

#### Public Tours
- [ ] Set house to public
- [ ] Search for public houses
- [ ] Visit another player's public house
- [ ] Rate a public house
- [ ] Verify visit count increments
- [ ] Verify average rating calculates correctly
- [ ] Verify you cannot rate your own house

#### Edge Cases
- [ ] Try to purchase second house (should fail)
- [ ] Try to place non-furniture item (should fail)
- [ ] Fill all furniture slots and try to place more (should fail)
- [ ] Try to modify another player's house without permission (should fail)
- [ ] Verify proper cleanup when leaving house
- [ ] Test with multiple players in same house instance

## Implementation Priority

Recommended order of implementation:

1. **Client Packet Handlers** (Required for any client functionality)
2. **HouseWindow.cs** (Core UI for furniture management)
3. **Map Properties Editor** (Needed to create housing maps)
4. **Item Editor - Furniture Properties** (Needed to create furniture items)
5. **Event Command Editors** (Needed for game designers to create housing NPCs)
6. **FurnitureStorageWindow.cs** (Storage furniture functionality)
7. **PublicHouseBrowserWindow.cs** (Social features)

## Notes

- The client UI should follow existing Intersect UI patterns (look at BankWindow.cs, ShopWindow.cs, etc.)
- Use the existing drag-and-drop systems from inventory management
- Furniture visual rendering may require custom rendering logic on the map
- Consider adding furniture preview mode before placement
- Add confirmation dialogs for destructive actions (removing furniture, removing visitors)
- Implement proper error handling and user feedback messages

## Database Migration

Run the following command to apply the housing system migration:

```bash
cd /home/user/Intersect-Engine/Intersect.Server.Core
dotnet ef database update --context PlayerContext
```

This will create all necessary tables for the player housing system.
