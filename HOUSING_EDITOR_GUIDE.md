# Player Housing System - Editor Implementation Guide

This guide provides detailed instructions for implementing the editor UI components for the player housing system.

## Overview

The editor needs UI components for:
1. Map Properties - Personal Instance Map flag
2. Item Editor - Furniture properties panel
3. Event Command Editors - 10 housing command forms

## 1. Map Properties Editor

**File:** `Intersect.Editor/Forms/Editors/frmMapProperties.cs` (and `.Designer.cs`)

### Required Change

Add a checkbox to the map properties form to set the `IsPersonalInstanceMap` flag.

**UI Element to Add:**
```csharp
private CheckBox chkPersonalInstanceMap;
```

**In the Designer.cs InitializeComponent():**
```csharp
// Add this control to the form
this.chkPersonalInstanceMap = new System.Windows.Forms.CheckBox();
this.chkPersonalInstanceMap.AutoSize = true;
this.chkPersonalInstanceMap.Location = new System.Drawing.Point(x, y); // Adjust as needed
this.chkPersonalInstanceMap.Name = "chkPersonalInstanceMap";
this.chkPersonalInstanceMap.Size = new System.Drawing.Size(width, height);
this.chkPersonalInstanceMap.TabIndex = n; // Set appropriate tab index
this.chkPersonalInstanceMap.Text = "Personal Instance Map (For Player Housing)";
this.chkPersonalInstanceMap.UseVisualStyleBackColor = true;

// Add to the form's controls
this.Controls.Add(this.chkPersonalInstanceMap);
```

**In the frmMapProperties.cs Load Method:**
```csharp
// When loading map properties
if (mEditorMap != null)
{
    chkPersonalInstanceMap.Checked = mEditorMap.IsPersonalInstanceMap;
}
```

**In the Save/OK Button Click Handler:**
```csharp
// When saving map properties
if (mEditorMap != null)
{
    mEditorMap.IsPersonalInstanceMap = chkPersonalInstanceMap.Checked;
}
```

**Location Suggestion:**
Add this checkbox in the "General" or "Advanced" section of the map properties form, near other map-wide settings.

---

## 2. Item Editor - Furniture Properties

**File:** `Intersect.Editor/Forms/Editors/frmItem.cs` (and `.Designer.cs`)

### Required Changes

Add a furniture properties panel with the following controls:

**UI Elements to Add:**
```csharp
private GroupBox grpFurniture;
private CheckBox chkCanBeFurniture;
private ComboBox cmbFurnitureType;
private Label lblFurnitureType;
private NumericUpDown nudWidth;
private Label lblWidth;
private NumericUpDown nudHeight;
private Label lblHeight;
private NumericUpDown nudZLayer;
private Label lblZLayer;
private CheckBox chkIsBlocking;
private TextBox txtPlacedSprite;
private Label lblPlacedSprite;
private NumericUpDown nudStorageSlots;
private Label lblStorageSlots;
private ComboBox cmbCraftingTable;
private Label lblCraftingTable;
private ComboBox cmbShop;
private Label lblShop;
private ComboBox cmbInteractionEvent;
private Label lblInteractionEvent;
private TextBox txtBuffEffects;
private Label lblBuffEffects;
```

**Layout Structure:**
```
GroupBox: grpFurniture
  CheckBox: chkCanBeFurniture
  Label: lblFurnitureType | ComboBox: cmbFurnitureType
  Label: lblWidth | NumericUpDown: nudWidth
  Label: lblHeight | NumericUpDown: nudHeight
  Label: lblZLayer | NumericUpDown: nudZLayer
  CheckBox: chkIsBlocking
  Label: lblPlacedSprite | TextBox: txtPlacedSprite

  --- Conditional Controls (show based on furniture type) ---
  Label: lblStorageSlots | NumericUpDown: nudStorageSlots (Storage type)
  Label: lblCraftingTable | ComboBox: cmbCraftingTable (CraftingStation type)
  Label: lblShop | ComboBox: cmbShop (ShopAccess type)
  Label: lblInteractionEvent | ComboBox: cmbInteractionEvent (Interactive type)
  Label: lblBuffEffects | TextBox: txtBuffEffects (Buff type)
```

**Initialization Code:**
```csharp
// Populate furniture type combo box
cmbFurnitureType.Items.Clear();
cmbFurnitureType.Items.Add("Decorative");
cmbFurnitureType.Items.Add("Storage");
cmbFurnitureType.Items.Add("Crafting Station");
cmbFurnitureType.Items.Add("Interactive");
cmbFurnitureType.Items.Add("Buff");
cmbFurnitureType.Items.Add("Bank Access");
cmbFurnitureType.Items.Add("Shop Access");
cmbFurnitureType.SelectedIndex = 0;

// Set up numeric up/down ranges
nudWidth.Minimum = 1;
nudWidth.Maximum = 10;
nudWidth.Value = 1;

nudHeight.Minimum = 1;
nudHeight.Maximum = 10;
nudHeight.Value = 1;

nudZLayer.Minimum = 0;
nudZLayer.Maximum = 10;
nudZLayer.Value = 0;

nudStorageSlots.Minimum = 1;
nudStorageSlots.Maximum = 100;
nudStorageSlots.Value = 20;

// Event handler for furniture type change
cmbFurnitureType.SelectedIndexChanged += CmbFurnitureType_SelectedIndexChanged;
chkCanBeFurniture.CheckedChanged += ChkCanBeFurniture_CheckedChanged;
```

**Event Handlers:**
```csharp
private void ChkCanBeFurniture_CheckedChanged(object sender, EventArgs e)
{
    // Enable/disable furniture properties based on checkbox
    cmbFurnitureType.Enabled = chkCanBeFurniture.Checked;
    nudWidth.Enabled = chkCanBeFurniture.Checked;
    nudHeight.Enabled = chkCanBeFurniture.Checked;
    nudZLayer.Enabled = chkCanBeFurniture.Checked;
    chkIsBlocking.Enabled = chkCanBeFurniture.Checked;
    txtPlacedSprite.Enabled = chkCanBeFurniture.Checked;

    UpdateFurnitureTypeControls();
}

private void CmbFurnitureType_SelectedIndexChanged(object sender, EventArgs e)
{
    UpdateFurnitureTypeControls();
}

private void UpdateFurnitureTypeControls()
{
    if (!chkCanBeFurniture.Checked)
    {
        // Hide all type-specific controls
        lblStorageSlots.Visible = false;
        nudStorageSlots.Visible = false;
        lblCraftingTable.Visible = false;
        cmbCraftingTable.Visible = false;
        lblShop.Visible = false;
        cmbShop.Visible = false;
        lblInteractionEvent.Visible = false;
        cmbInteractionEvent.Visible = false;
        lblBuffEffects.Visible = false;
        txtBuffEffects.Visible = false;
        return;
    }

    // Hide all first
    lblStorageSlots.Visible = false;
    nudStorageSlots.Visible = false;
    lblCraftingTable.Visible = false;
    cmbCraftingTable.Visible = false;
    lblShop.Visible = false;
    cmbShop.Visible = false;
    lblInteractionEvent.Visible = false;
    cmbInteractionEvent.Visible = false;
    lblBuffEffects.Visible = false;
    txtBuffEffects.Visible = false;

    // Show controls based on selected type
    switch (cmbFurnitureType.SelectedIndex)
    {
        case 1: // Storage
            lblStorageSlots.Visible = true;
            nudStorageSlots.Visible = true;
            break;
        case 2: // Crafting Station
            lblCraftingTable.Visible = true;
            cmbCraftingTable.Visible = true;
            break;
        case 3: // Interactive
            lblInteractionEvent.Visible = true;
            cmbInteractionEvent.Visible = true;
            break;
        case 4: // Buff
            lblBuffEffects.Visible = true;
            txtBuffEffects.Visible = true;
            break;
        case 5: // Bank Access
            // No additional properties needed
            break;
        case 6: // Shop Access
            lblShop.Visible = true;
            cmbShop.Visible = true;
            break;
    }
}
```

**Load Item Data:**
```csharp
private void LoadItemData()
{
    if (mEditorItem == null) return;

    chkCanBeFurniture.Checked = mEditorItem.CanBeFurniture;

    if (mEditorItem.FurnitureProperties != null)
    {
        cmbFurnitureType.SelectedIndex = (int)mEditorItem.FurnitureProperties.Type;
        nudWidth.Value = mEditorItem.FurnitureProperties.Width;
        nudHeight.Value = mEditorItem.FurnitureProperties.Height;
        nudZLayer.Value = mEditorItem.FurnitureProperties.ZLayer;
        chkIsBlocking.Checked = mEditorItem.FurnitureProperties.IsBlocking;
        txtPlacedSprite.Text = mEditorItem.FurnitureProperties.PlacedSprite ?? "";
        nudStorageSlots.Value = mEditorItem.FurnitureProperties.StorageSlots;

        // Load combo box selections
        // (Implementation depends on how you populate these combo boxes)
    }

    UpdateFurnitureTypeControls();
}
```

**Save Item Data:**
```csharp
private void SaveItemData()
{
    if (mEditorItem == null) return;

    mEditorItem.CanBeFurniture = chkCanBeFurniture.Checked;

    if (chkCanBeFurniture.Checked)
    {
        if (mEditorItem.FurnitureProperties == null)
        {
            mEditorItem.FurnitureProperties = new FurnitureProperties();
        }

        mEditorItem.FurnitureProperties.Type = (FurnitureType)cmbFurnitureType.SelectedIndex;
        mEditorItem.FurnitureProperties.Width = (int)nudWidth.Value;
        mEditorItem.FurnitureProperties.Height = (int)nudHeight.Value;
        mEditorItem.FurnitureProperties.ZLayer = (int)nudZLayer.Value;
        mEditorItem.FurnitureProperties.IsBlocking = chkIsBlocking.Checked;
        mEditorItem.FurnitureProperties.PlacedSprite = txtPlacedSprite.Text;
        mEditorItem.FurnitureProperties.StorageSlots = (int)nudStorageSlots.Value;

        // Save combo box selections
        // CraftingTableId, ShopId, InteractionEventId, BuffEffects
    }
    else
    {
        mEditorItem.FurnitureProperties = null;
    }
}
```

---

## 3. Event Command Editors

All event command editors should be placed in:
**Directory:** `Intersect.Editor/Forms/Editors/Events/Event Commands/`

### Pattern for Event Command Editors

Each editor follows this general structure:

```csharp
public partial class EventCommand_[CommandName] : UserControl
{
    private EventCommand mMyCommand;
    private EventPage mCurrentPage;

    public EventCommand_[CommandName](EventCommand command, EventPage page)
    {
        InitializeComponent();

        mMyCommand = command;
        mCurrentPage = page;

        InitEditor();
    }

    private void InitEditor()
    {
        // Load command data into UI controls
    }

    private void UpdateCommand()
    {
        // Save UI control data back to command
    }
}
```

### A. EventCommand_PurchaseHouse.cs

**Controls Needed:**
- ComboBox: `cmbMap` (List of maps with IsPersonalInstanceMap = true)
- ComboBox: `cmbCurrency` (List of items that can be used as currency)
- NumericUpDown: `nudCost` (Cost amount)
- Label controls for each

**Code:**
```csharp
private void InitEditor()
{
    // Populate maps
    cmbMap.Items.Clear();
    foreach (var map in MapDescriptor.Descriptors)
    {
        if (map.IsPersonalInstanceMap)
        {
            cmbMap.Items.Add(map.Name);
        }
    }

    // Populate currency items
    cmbCurrency.Items.Clear();
    foreach (var item in ItemDescriptor.Descriptors)
    {
        cmbCurrency.Items.Add(item.Name);
    }

    // Load from command
    var cmd = (PurchaseHouseCommand)mMyCommand;
    // Set selected items based on cmd.MapId, cmd.CurrencyId
    nudCost.Value = cmd.Cost;
}

private void UpdateCommand()
{
    var cmd = (PurchaseHouseCommand)mMyCommand;
    // Save selected map and currency IDs
    cmd.Cost = (int)nudCost.Value;
}
```

### B. EventCommand_EnterHouse.cs

**Controls Needed:**
- RadioButton: `rbOwnHouse` (Enter own house)
- RadioButton: `rbSpecificPlayer` (Enter specific player's house)
- RadioButton: `rbPlayerVariable` (Get player ID from variable)
- TextBox: `txtTargetPlayerId` (For specific player GUID)
- ComboBox: `cmbPlayerVariable` (For player variable selection)
- NumericUpDown: `nudX` (Spawn X position)
- NumericUpDown: `nudY` (Spawn Y position)

**Code:**
```csharp
private void InitEditor()
{
    var cmd = (EnterHouseCommand)mMyCommand;

    // Populate player variables
    cmbPlayerVariable.Items.Clear();
    foreach (var variable in PlayerVariableDescriptor.Descriptors)
    {
        cmbPlayerVariable.Items.Add(variable.Name);
    }

    // Load settings
    if (cmd.TargetPlayerId != Guid.Empty)
    {
        rbSpecificPlayer.Checked = true;
        txtTargetPlayerId.Text = cmd.TargetPlayerId.ToString();
    }
    else if (cmd.PlayerVariableId != Guid.Empty)
    {
        rbPlayerVariable.Checked = true;
        // Select variable in combo
    }
    else
    {
        rbOwnHouse.Checked = true;
    }

    nudX.Value = cmd.X;
    nudY.Value = cmd.Y;

    UpdateControlStates();
}

private void UpdateControlStates()
{
    txtTargetPlayerId.Enabled = rbSpecificPlayer.Checked;
    cmbPlayerVariable.Enabled = rbPlayerVariable.Checked;
}
```

### C. EventCommand_OpenHouseFurniture.cs

**Simple command with no parameters - just a confirmation message**

```csharp
public partial class EventCommand_OpenHouseFurniture : UserControl
{
    public EventCommand_OpenHouseFurniture(EventCommand command, EventPage page)
    {
        InitializeComponent();

        Label lblInfo = new Label();
        lblInfo.Text = "This command opens the house furniture interface for the player.";
        lblInfo.Dock = DockStyle.Fill;
        lblInfo.TextAlign = ContentAlignment.MiddleCenter;
        this.Controls.Add(lblInfo);
    }
}
```

### D. EventCommand_InviteToHouse.cs

**Controls:**
- ComboBox: `cmbPlayerVariable` (Select player variable containing target player ID)
- ComboBox: `cmbPermission` (View, Modify)

**Code:**
```csharp
private void InitEditor()
{
    var cmd = (InviteToHouseCommand)mMyCommand;

    cmbPlayerVariable.Items.Clear();
    foreach (var variable in PlayerVariableDescriptor.Descriptors)
    {
        cmbPlayerVariable.Items.Add(variable.Name);
    }

    cmbPermission.Items.Clear();
    cmbPermission.Items.Add("View");
    cmbPermission.Items.Add("Modify");
    cmbPermission.SelectedIndex = (int)cmd.Permission - 1; // Skip None enum value
}
```

### E. EventCommand_RemoveHouseVisitor.cs

**Controls:**
- ComboBox: `cmbPlayerVariable` (Select player variable containing target player ID)

**Code:**
```csharp
private void InitEditor()
{
    var cmd = (RemoveHouseVisitorCommand)mMyCommand;

    cmbPlayerVariable.Items.Clear();
    foreach (var variable in PlayerVariableDescriptor.Descriptors)
    {
        cmbPlayerVariable.Items.Add(variable.Name);
    }

    // Select current variable
}
```

### F. EventCommand_SetHousePublic.cs

**Controls:**
- CheckBox: `chkIsPublic` (Set house public/private)

**Code:**
```csharp
private void InitEditor()
{
    var cmd = (SetHousePublicCommand)mMyCommand;
    chkIsPublic.Checked = cmd.IsPublic;
}

private void UpdateCommand()
{
    var cmd = (SetHousePublicCommand)mMyCommand;
    cmd.IsPublic = chkIsPublic.Checked;
}
```

### G. EventCommand_SetHouseName.cs

**Controls:**
- ComboBox: `cmbNameVariable` (Select string variable containing house name)

**Code:**
```csharp
private void InitEditor()
{
    var cmd = (SetHouseNameCommand)mMyCommand;

    cmbNameVariable.Items.Clear();
    foreach (var variable in PlayerVariableDescriptor.Descriptors)
    {
        if (variable.DataType == VariableDataType.String)
        {
            cmbNameVariable.Items.Add(variable.Name);
        }
    }

    // Select current variable
}
```

### H. EventCommand_SetHouseDescription.cs

**Controls:**
- ComboBox: `cmbDescriptionVariable` (Select string variable containing description)

**Code:**
```csharp
// Similar to SetHouseName
```

### I. EventCommand_RateHouse.cs

**Controls:**
- ComboBox: `cmbRatingVariable` (Select integer variable containing rating 1-5)

**Code:**
```csharp
private void InitEditor()
{
    var cmd = (RateHouseCommand)mMyCommand;

    cmbRatingVariable.Items.Clear();
    foreach (var variable in PlayerVariableDescriptor.Descriptors)
    {
        if (variable.DataType == VariableDataType.Integer)
        {
            cmbRatingVariable.Items.Add(variable.Name);
        }
    }

    // Select current variable
}
```

### J. EventCommand_OpenFurnitureStorage.cs

**Controls:**
- ComboBox: `cmbFurnitureSlotVariable` (Select integer variable containing furniture slot index)

**Code:**
```csharp
private void InitEditor()
{
    var cmd = (OpenFurnitureStorageCommand)mMyCommand;

    cmbFurnitureSlotVariable.Items.Clear();
    foreach (var variable in PlayerVariableDescriptor.Descriptors)
    {
        if (variable.DataType == VariableDataType.Integer)
        {
            cmbFurnitureSlotVariable.Items.Add(variable.Name);
        }
    }

    // Select current variable
}
```

---

## 4. Registering Event Commands

**File:** `Intersect.Editor/Forms/Editors/Events/EventCommandList.cs` (or similar)

Add the housing commands to the event command list/menu:

```csharp
// In the command list initialization
var housingMenu = new ToolStripMenuItem("Player Housing");

housingMenu.DropDownItems.Add(new ToolStripMenuItem("Purchase House", null, (s, e) => AddCommand(EventCommandType.PurchaseHouse)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Enter House", null, (s, e) => AddCommand(EventCommandType.EnterHouse)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Open House Furniture", null, (s, e) => AddCommand(EventCommandType.OpenHouseFurniture)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Invite to House", null, (s, e) => AddCommand(EventCommandType.InviteToHouse)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Remove House Visitor", null, (s, e) => AddCommand(EventCommandType.RemoveHouseVisitor)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Set House Public", null, (s, e) => AddCommand(EventCommandType.SetHousePublic)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Set House Name", null, (s, e) => AddCommand(EventCommandType.SetHouseName)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Set House Description", null, (s, e) => AddCommand(EventCommandType.SetHouseDescription)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Rate House", null, (s, e) => AddCommand(EventCommandType.RateHouse)));
housingMenu.DropDownItems.Add(new ToolStripMenuItem("Open Furniture Storage", null, (s, e) => AddCommand(EventCommandType.OpenFurnitureStorage)));

// Add housing menu to the main command menu
commandMenu.Items.Add(housingMenu);
```

---

## 5. Testing the Editor Components

### Test Map Properties:
1. Open a map in the editor
2. Check "Personal Instance Map"
3. Save the map
4. Reopen - verify the checkbox is still checked
5. Create an event that uses "Purchase House" command referencing this map

### Test Item Editor:
1. Create a new item
2. Check "Can Be Furniture"
3. Select furniture type "Storage"
4. Set storage slots to 20
5. Save the item
6. Reopen - verify all furniture properties are saved

### Test Event Commands:
1. Create a new NPC event
2. Add each housing command
3. Configure properties
4. Save and reopen
5. Verify all command properties are saved correctly

---

## Notes

- All Windows Forms Designer files (`.Designer.cs`) should be edited through the Visual Studio designer when possible
- Maintain consistent styling with existing editor components
- Use the existing localization system for all UI strings
- Follow the existing event command editor patterns
- Test thoroughly after each component is added

## Additional Resources

- Reference existing event command editors in the same directory
- Check BankInterface/ShopInterface implementations for UI patterns
- See Guild system for permission-based UI examples
