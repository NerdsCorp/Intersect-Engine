using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.Inventory;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.Network.Packets.Client;

namespace Intersect.Client.Interface.Game.Housing;

public partial class FurnitureStorageItem : SlotItem
{
    // Controls
    private readonly Label _quantityLabel;
    private FurnitureStorageWindow _storageWindow;

    // Context Menu Handling
    private MenuItem _withdrawContextItem;

    public FurnitureStorageItem(FurnitureStorageWindow storageWindow, Base parent, int index, ContextMenu contextMenu) :
        base(parent, nameof(FurnitureStorageItem), index, contextMenu)
    {
        _storageWindow = storageWindow;
        TextureFilename = "furniturestorageitem.png";

        Icon.HoverEnter += Icon_HoverEnter;
        Icon.HoverLeave += Icon_HoverLeave;
        Icon.Clicked += Icon_Clicked;
        Icon.DoubleClicked += Icon_DoubleClicked;

        _quantityLabel = new Label(this, "Quantity")
        {
            Alignment = [Alignments.Bottom, Alignments.Right],
            BackgroundTemplateName = "quantity.png",
            FontName = "sourcesansproblack",
            FontSize = 8,
            Padding = new Padding(2),
        };

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

        contextMenu.ClearChildren();
        _withdrawContextItem = contextMenu.AddItem("Withdraw");
        _withdrawContextItem.Clicked += _withdrawMenuItem_Clicked;
        contextMenu.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    #region Context Menu

    protected override void OnContextMenuOpening(ContextMenu contextMenu)
    {
        if (Globals.FurnitureStorageSlots is not { Length: > 0 } storageSlots)
        {
            return;
        }

        if (!ItemDescriptor.TryGet(storageSlots[SlotIndex].ItemId, out var item))
        {
            return;
        }

        // Clear the context menu and add the withdraw item with updated item name
        contextMenu.ClearChildren();
        contextMenu.AddChild(_withdrawContextItem);
        _withdrawContextItem.SetText($"Withdraw {item.Name}");

        base.OnContextMenuOpening(contextMenu);
    }

    private void _withdrawMenuItem_Clicked(Base sender, MouseButtonState arguments)
    {
        // Send packet to withdraw from storage
        if (Globals.FurnitureStorageSlots is not { Length: > 0 } storageSlots)
        {
            return;
        }

        if (storageSlots[SlotIndex] is not { Quantity: > 0 } slot)
        {
            return;
        }

        // Withdraw the item
        PacketSender.SendFurnitureStorageInteraction(
            FurnitureStorageInteractionPacket.ActionType.Withdraw,
            SlotIndex,
            slot.Quantity
        );
    }

    #endregion

    #region Mouse Events

    private void Icon_HoverEnter(Base? sender, EventArgs? arguments)
    {
        if (InputHandler.MouseFocus != null)
        {
            return;
        }

        if (Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
        {
            return;
        }

        if (Globals.FurnitureStorageSlots is not { Length: > 0 } storageSlots)
        {
            return;
        }

        if (storageSlots[SlotIndex] is not { Descriptor: not null } or { Quantity: <= 0 })
        {
            return;
        }

        var item = storageSlots[SlotIndex];
        Interface.GameUi.ItemDescriptionWindow?.Show(item.Descriptor, item.Quantity, item.ItemProperties);
    }

    private void Icon_HoverLeave(Base sender, EventArgs arguments)
    {
        Interface.GameUi.ItemDescriptionWindow?.Hide();
    }

    private void Icon_Clicked(Base sender, MouseButtonState arguments)
    {
        if (arguments.MouseButton is MouseButton.Right)
        {
            if (ClientConfiguration.Instance.EnableContextMenus)
            {
                OpenContextMenu();
            }
            else
            {
                Icon_DoubleClicked(sender, arguments);
            }
        }
    }

    private void Icon_DoubleClicked(Base sender, MouseButtonState arguments)
    {
        if (arguments.MouseButton is not MouseButton.Left)
        {
            return;
        }

        if (Globals.FurnitureStorageSlots is not { Length: > 0 } storageSlots)
        {
            return;
        }

        if (storageSlots[SlotIndex] is not { Quantity: > 0 } slot)
        {
            return;
        }

        // Double-click withdraws entire stack
        PacketSender.SendFurnitureStorageInteraction(
            FurnitureStorageInteractionPacket.ActionType.Withdraw,
            SlotIndex,
            slot.Quantity
        );
    }

    #endregion

    #region Drag and Drop

    public override bool DragAndDrop_HandleDrop(Package package, int x, int y)
    {
        if (Globals.Me is not { } player)
        {
            return false;
        }

        var targetNode = Interface.FindComponentUnderCursor();

        // Find the first parent acceptable in that tree that can accept the package
        while (targetNode != default)
        {
            switch (targetNode)
            {
                case FurnitureStorageItem storageItem:
                    // Swap storage items - not supported for now
                    ChatboxMsg.AddMessage(
                        new ChatboxMsg(
                            "Cannot swap storage items directly.",
                            CustomColors.Alerts.Info,
                            ChatMessageType.Notice
                        )
                    );
                    return false;

                case InventoryItem inventoryItem:
                    // Depositing from inventory to storage
                    if (Globals.Me.Inventory is not { Length: > 0 } inventory)
                    {
                        return false;
                    }

                    if (inventoryItem.SlotIndex >= inventory.Length)
                    {
                        return false;
                    }

                    var inventorySlot = inventory[inventoryItem.SlotIndex];
                    if (inventorySlot is not { Quantity: > 0 })
                    {
                        return false;
                    }

                    // Deposit the item
                    PacketSender.SendFurnitureStorageInteraction(
                        FurnitureStorageInteractionPacket.ActionType.Deposit,
                        inventoryItem.SlotIndex,
                        inventorySlot.Quantity
                    );
                    return true;

                default:
                    targetNode = targetNode.Parent;
                    break;
            }
        }

        // If we've reached the top of the tree, we can't drop here, so cancel drop
        return false;
    }

    #endregion

    public override void Update()
    {
        if (Globals.Me == default)
        {
            return;
        }

        if (Globals.FurnitureStorageSlots is not { Length: > 0 } storageSlots)
        {
            return;
        }

        if (storageSlots[SlotIndex] is not { Descriptor: not null } or { Quantity: <= 0 })
        {
            _quantityLabel.IsVisibleInParent = false;
            Icon.Texture = default;
            return;
        }

        var storageSlot = storageSlots[SlotIndex];
        var descriptor = storageSlot.Descriptor;

        _quantityLabel.IsVisibleInParent = !Icon.IsDragging && descriptor.IsStackable && storageSlot.Quantity > 1;
        if (_quantityLabel.IsVisibleInParent)
        {
            _quantityLabel.Text = Strings.FormatQuantityAbbreviated(storageSlot.Quantity);
        }

        if (Icon.TextureFilename == descriptor.Icon)
        {
            return;
        }

        var itemTexture = GameContentManager.Current.GetTexture(Framework.Content.TextureType.Item, descriptor.Icon);
        if (itemTexture != default)
        {
            Icon.Texture = itemTexture;
            Icon.RenderColor = descriptor.Color;
            Icon.IsVisibleInParent = true;
        }
        else
        {
            if (Icon.Texture != default)
            {
                Icon.Texture = default;
                Icon.IsVisibleInParent = false;
            }
        }
    }
}
