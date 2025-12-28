using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Utilities;

namespace Intersect.Client.Interface.Game.Housing;

public partial class FurnitureStorageWindow : Window
{
    public List<SlotItem> Items = [];
    private readonly ScrollControl _slotContainer;
    private readonly ContextMenu _contextMenu;

    //Init
    public FurnitureStorageWindow(Canvas gameCanvas) : base(
        gameCanvas,
        "Furniture Storage",
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

        TitleLabel.FontSize = 14;
        TitleLabel.TextColorOverride = Color.White;

        Closed += (b, s) =>
        {
            _contextMenu?.Close();
            Interface.GameUi.NotifyCloseFurnitureStorage();
        };

        _slotContainer = new ScrollControl(this, "ItemContainer")
        {
            Dock = Pos.Fill,
            OverflowX = OverflowBehavior.Auto,
            OverflowY = OverflowBehavior.Scroll,
        };

        _contextMenu = new ContextMenu(gameCanvas, "FurnitureStorageContextMenu")
        {
            IsVisibleInParent = false,
            IconMarginDisabled = true,
            ItemFont = GameContentManager.Current.GetFont(name: "sourcesansproblack"),
            ItemFontSize = 10,
        };
    }

    protected override void EnsureInitialized()
    {
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
        InitItemContainer();
    }

    private void InitItemContainer()
    {
        for (var slotIndex = 0; slotIndex < Globals.FurnitureStorageSlotCount; slotIndex++)
        {
            Items.Add(new FurnitureStorageItem(this, _slotContainer, slotIndex, _contextMenu));
        }

        PopulateSlotContainer.Populate(_slotContainer, Items);
    }

    public void Update()
    {
        if (IsVisibleInTree == false)
        {
            return;
        }

        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i] is FurnitureStorageItem storageItem)
            {
                storageItem.Update();
            }
        }
    }

    public void UpdateStorageSlot(int slot)
    {
        if (slot < 0 || slot >= Items.Count)
        {
            return;
        }

        if (Items[slot] is FurnitureStorageItem storageItem)
        {
            storageItem.Update();
        }
    }

    public override void Hide()
    {
        _contextMenu?.Close();
        base.Hide();
    }
}
