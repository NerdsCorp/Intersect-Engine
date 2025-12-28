using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Game.Housing;

/// <summary>
/// Furniture Storage Window - Storage Container Interface
/// TODO: Complete implementation with full UI controls
/// Reference: BankWindow.cs for pattern
/// </summary>
public partial class FurnitureStorageWindow : Window
{
    // TODO: Add proper controls
    // public List<SlotItem> StorageSlots = [];
    // private readonly ScrollControl _slotContainer;
    // private readonly ContextMenu _contextMenu;

    private readonly Label _todoLabel;

    public FurnitureStorageWindow(Canvas gameCanvas) : base(
        gameCanvas,
        "Furniture Storage", // TODO: Use Strings.FurnitureStorage.Title when localization is added
        false,
        nameof(FurnitureStorageWindow)
    )
    {
        DisableResizing();
        Interface.InputBlockingComponents.Add(this);

        Alignment = [Alignments.Center];
        MinimumSize = new Point(x: 436, y: 454);
        IsResizable = false;
        IsClosable = true;

        Closed += (b, s) =>
        {
            Interface.GameUi.NotifyCloseFurnitureStorage();
        };

        // TODO: Remove this temporary label and implement actual UI
        _todoLabel = new Label(this)
        {
            Text = "Furniture Storage Window - Under Construction\n\n" +
                   "TODO: Implement storage slot grid\n" +
                   "TODO: Add drag-and-drop from/to inventory\n" +
                   "TODO: Add deposit/withdraw functionality\n" +
                   "TODO: Add stack splitting support\n\n" +
                   "See HOUSING_CLIENT_TODO.md for implementation details\n" +
                   "Pattern similar to BankWindow.cs",
            Dock = Pos.Fill,
            Alignment = Pos.Center,
            TextAlign = Pos.Center
        };

        // TODO: Initialize actual controls
        // _slotContainer = new ScrollControl(this, "StorageContainer") { ... };
        // _contextMenu = new ContextMenu(gameCanvas, "StorageContextMenu") { ... };
        // InitStorageSlots();
    }

    // TODO: Implement
    // protected override void EnsureInitialized()
    // {
    //     LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    //     InitStorageSlots();
    // }

    // TODO: Implement
    // private void InitStorageSlots()
    // {
    //     for (var slotIndex = 0; slotIndex < Globals.FurnitureStorageSlotCount; slotIndex++)
    //     {
    //         StorageSlots.Add(new FurnitureStorageItem(this, _slotContainer, slotIndex, _contextMenu));
    //     }
    //     PopulateSlotContainer.Populate(_slotContainer, StorageSlots);
    // }

    public void Update()
    {
        if (IsVisibleInTree == false)
        {
            return;
        }

        // TODO: Update storage slots
        // for (var i = 0; i < StorageSlots.Count; i++)
        // {
        //     if (StorageSlots[i] is FurnitureStorageItem item)
        //     {
        //         item.Update();
        //     }
        // }
    }

    public void UpdateStorageSlot(int slot)
    {
        // TODO: Update specific storage slot when server sends update
        // if (slot >= 0 && slot < StorageSlots.Count)
        // {
        //     StorageSlots[slot].Update();
        // }
    }

    public override void Hide()
    {
        // TODO: Close context menu when implemented
        // _contextMenu?.Close();
        base.Hide();
    }
}
