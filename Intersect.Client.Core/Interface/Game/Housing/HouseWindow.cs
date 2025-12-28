using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Game.Housing;

/// <summary>
/// House Window - Furniture Management Interface
/// TODO: Complete implementation with full UI controls
/// Reference: BankWindow.cs for pattern
/// </summary>
public partial class HouseWindow : Window
{
    // TODO: Add proper controls
    // public List<SlotItem> FurnitureSlots = [];
    // private readonly ScrollControl _slotContainer;
    // private readonly ContextMenu _contextMenu;

    private readonly Label _todoLabel;

    public HouseWindow(Canvas gameCanvas) : base(
        gameCanvas,
        "House Furniture", // TODO: Use Strings.Houses.Title when localization is added
        false,
        nameof(HouseWindow)
    )
    {
        DisableResizing();
        Interface.InputBlockingComponents.Add(this);

        Alignment = [Alignments.Center];
        MinimumSize = new Point(x: 600, y: 500);
        IsResizable = false;
        IsClosable = true;

        Closed += (b, s) =>
        {
            Interface.GameUi.NotifyCloseHouse();
        };

        // TODO: Remove this temporary label and implement actual UI
        _todoLabel = new Label(this)
        {
            Text = "House Furniture Window - Under Construction\n\n" +
                   "TODO: Implement furniture slot grid\n" +
                   "TODO: Add drag-and-drop from inventory\n" +
                   "TODO: Add furniture positioning controls\n" +
                   "TODO: Add remove furniture functionality\n\n" +
                   "See HOUSING_CLIENT_TODO.md for implementation details",
            Dock = Pos.Fill,
            Alignment = Pos.Center,
            TextAlign = Pos.Center
        };

        // TODO: Initialize actual controls
        // _slotContainer = new ScrollControl(this, "FurnitureContainer") { ... };
        // _contextMenu = new ContextMenu(gameCanvas, "HouseContextMenu") { ... };
        // InitFurnitureSlots();
    }

    // TODO: Implement
    // protected override void EnsureInitialized()
    // {
    //     LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    //     InitFurnitureSlots();
    // }

    // TODO: Implement
    // private void InitFurnitureSlots()
    // {
    //     for (var slotIndex = 0; slotIndex < Globals.HouseFurnitureSlotCount; slotIndex++)
    //     {
    //         FurnitureSlots.Add(new HouseFurnitureItem(this, _slotContainer, slotIndex, _contextMenu));
    //     }
    //     PopulateSlotContainer.Populate(_slotContainer, FurnitureSlots);
    // }

    public void Update()
    {
        if (IsVisibleInTree == false)
        {
            return;
        }

        // TODO: Update furniture slots
        // for (var i = 0; i < FurnitureSlots.Count; i++)
        // {
        //     if (FurnitureSlots[i] is HouseFurnitureItem item)
        //     {
        //         item.Update();
        //     }
        // }
    }

    public void UpdateFurnitureSlot(int slot)
    {
        // TODO: Update specific furniture slot when server sends update
        // if (slot >= 0 && slot < FurnitureSlots.Count)
        // {
        //     FurnitureSlots[slot].Update();
        // }
    }

    public override void Hide()
    {
        // TODO: Close context menu when implemented
        // _contextMenu?.Close();
        base.Hide();
    }
}
