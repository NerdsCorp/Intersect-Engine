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

public partial class HouseFurnitureItem : SlotItem
{
    // Controls
    private readonly Label _quantityLabel;
    private HouseWindow _houseWindow;

    // Context Menu Handling
    private MenuItem _removeContextItem;
    private MenuItem _moveContextItem;

    public HouseFurnitureItem(HouseWindow houseWindow, Base parent, int index, ContextMenu contextMenu) :
        base(parent, nameof(HouseFurnitureItem), index, contextMenu)
    {
        _houseWindow = houseWindow;
        TextureFilename = "housefurnitureitem.png";

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
        _removeContextItem = contextMenu.AddItem("Remove Furniture");
        _removeContextItem.Clicked += _removeMenuItem_Clicked;
        _moveContextItem = contextMenu.AddItem("Move Furniture");
        _moveContextItem.Clicked += _moveMenuItem_Clicked;
        contextMenu.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    #region Context Menu

    protected override void OnContextMenuOpening(ContextMenu contextMenu)
    {
        if (Globals.HouseFurnitureSlots is not { Length: > 0 } furnitureSlots)
        {
            return;
        }

        if (!ItemDescriptor.TryGet(furnitureSlots[SlotIndex].ItemId, out var item))
        {
            return;
        }

        // Clear the context menu and add items with updated item name
        contextMenu.ClearChildren();
        contextMenu.AddChild(_removeContextItem);
        contextMenu.AddChild(_moveContextItem);
        _removeContextItem.SetText($"Remove {item.Name}");
        _moveContextItem.SetText($"Move {item.Name}");

        base.OnContextMenuOpening(contextMenu);
    }

    private void _removeMenuItem_Clicked(Base sender, MouseButtonState arguments)
    {
        // Send packet to remove furniture at this slot
        PacketSender.SendHouseFurnitureAction(
            HouseFurnitureActionPacket.ActionType.Remove,
            -1, // No inventory slot
            SlotIndex,
            0, 0, 0 // No position/direction for removal
        );
    }

    private void _moveMenuItem_Clicked(Base sender, MouseButtonState arguments)
    {
        // TODO: Implement furniture movement UI
        // This would require a furniture placement mode where user clicks on the map
        ChatboxMsg.AddMessage(
            new ChatboxMsg(
                "Furniture movement coming soon! For now, remove and replace furniture to reposition it.",
                CustomColors.Alerts.Info,
                ChatMessageType.Notice
            )
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

        if (Globals.HouseFurnitureSlots is not { Length: > 0 } furnitureSlots)
        {
            return;
        }

        if (furnitureSlots[SlotIndex] is not { Descriptor: not null } or { Quantity: <= 0 })
        {
            return;
        }

        var item = furnitureSlots[SlotIndex];
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

        if (Globals.HouseFurnitureSlots is not { Length: > 0 } furnitureSlots)
        {
            return;
        }

        if (furnitureSlots[SlotIndex] is not { Quantity: > 0 })
        {
            return;
        }

        // Double-click removes furniture
        PacketSender.SendHouseFurnitureAction(
            HouseFurnitureActionPacket.ActionType.Remove,
            -1, // No inventory slot
            SlotIndex,
            0, 0, 0 // No position/direction for removal
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
                case HouseFurnitureItem furnitureItem:
                    // Swap furniture positions
                    // For now, we'll just reject this - swapping furniture slots isn't supported
                    ChatboxMsg.AddMessage(
                        new ChatboxMsg(
                            "Cannot swap furniture directly. Remove furniture first, then place new furniture.",
                            CustomColors.Alerts.Info,
                            ChatMessageType.Notice
                        )
                    );
                    return false;

                case InventoryItem inventoryItem:
                    // Placing furniture from inventory
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

                    // Check if item can be furniture
                    if (!ItemDescriptor.TryGet(inventorySlot.ItemId, out var item) || !item.CanBeFurniture)
                    {
                        ChatboxMsg.AddMessage(
                            new ChatboxMsg(
                                "This item cannot be placed as furniture.",
                                CustomColors.Alerts.Error,
                                ChatMessageType.Notice
                            )
                        );
                        return false;
                    }

                    // Send packet to place furniture
                    // TODO: For full implementation, this should open a placement UI on the map
                    // For now, we'll just place it at default position (0, 0)
                    PacketSender.SendHouseFurnitureAction(
                        HouseFurnitureActionPacket.ActionType.Place,
                        inventoryItem.SlotIndex,
                        SlotIndex,
                        0, 0, 0 // Position and direction - TODO: Get from placement UI
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

        if (Globals.HouseFurnitureSlots is not { Length: > 0 } furnitureSlots)
        {
            return;
        }

        if (furnitureSlots[SlotIndex] is not { Descriptor: not null } or { Quantity: <= 0 })
        {
            _quantityLabel.IsVisibleInParent = false;
            Icon.Texture = default;
            return;
        }

        var furnitureSlot = furnitureSlots[SlotIndex];
        var descriptor = furnitureSlot.Descriptor;

        _quantityLabel.IsVisibleInParent = !Icon.IsDragging && descriptor.IsStackable && furnitureSlot.Quantity > 1;
        if (_quantityLabel.IsVisibleInParent)
        {
            _quantityLabel.Text = Strings.FormatQuantityAbbreviated(furnitureSlot.Quantity);
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
