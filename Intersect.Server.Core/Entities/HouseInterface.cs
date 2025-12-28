using Intersect.Collections.Slotting;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Network.Packets.Server;
using Intersect.Server.Database;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Localization;
using Intersect.Server.Networking;
using Microsoft.Extensions.Logging;

namespace Intersect.Server.Entities;

/// <summary>
/// Manages furniture placement and removal for a player's house.
/// </summary>
public partial class HouseInterface : IHouseInterface
{
    private readonly Player _player;
    private readonly PlayerHouse _house;
    private readonly SlotList<HouseFurnitureSlot> _furniture;
    private readonly object _lock;

    public HouseInterface(Player player, PlayerHouse house)
    {
        _player = player;
        _house = house;
        _furniture = house.Furniture;
        _lock = house.Lock;
    }

    public void SendOpenHouse()
    {
        var slotUpdatePackets = new List<HouseFurnitureUpdatePacket>();

        for (var slot = 0; slot < _furniture.Capacity; slot++)
        {
            var furnitureSlot = slot < _furniture.Count ? _furniture[slot] : default;
            slotUpdatePackets.Add(
                new HouseFurnitureUpdatePacket(
                    slot,
                    furnitureSlot?.ItemId ?? Guid.Empty,
                    furnitureSlot?.Quantity ?? 0,
                    furnitureSlot?.X ?? 0,
                    furnitureSlot?.Y ?? 0,
                    furnitureSlot?.Direction ?? 0,
                    furnitureSlot?.Properties
                )
            );
        }

        _player?.SendPacket(
            new HousePacket(
                false,
                _house.Id,
                _house.OwnerId,
                _furniture.Capacity,
                slotUpdatePackets.ToArray()
            )
        );
    }

    public void SendFurnitureUpdate(int slot, bool sendToAll = true)
    {
        // For now, just send to the current player
        // Future enhancement: send to all players currently in the house
        if (_furniture[slot] != null && _furniture[slot].ItemId != Guid.Empty)
        {
            _player?.SendPacket(
                new HouseFurnitureUpdatePacket(
                    slot,
                    _furniture[slot].ItemId,
                    _furniture[slot].Quantity,
                    _furniture[slot].X,
                    _furniture[slot].Y,
                    _furniture[slot].Direction,
                    _furniture[slot].Properties
                )
            );
        }
        else
        {
            _player?.SendPacket(new HouseFurnitureUpdatePacket(slot, Guid.Empty, 0, 0, 0, 0, null));
        }
    }

    public void SendCloseHouse()
    {
        _player?.SendPacket(new HousePacket(true, Guid.Empty, Guid.Empty, -1, null));
    }

    public bool TryPlaceFurniture(int inventorySlotIndex, int x, int y, int direction = 0)
    {
        // Check if player has permission to modify furniture
        var permission = _house.GetPermission(_player.Id);
        if (permission != HousePermission.Owner && permission != HousePermission.Modify)
        {
            PacketSender.SendChatMsg(
                _player,
                Strings.Houses.NoPermissionToModify,
                ChatMessageType.Error,
                CustomColors.Alerts.Error
            );
            return false;
        }

        var inventorySlot = _player.Items[inventorySlotIndex];
        if (inventorySlot == null || inventorySlot.ItemId == Guid.Empty)
        {
            PacketSender.SendChatMsg(
                _player,
                Strings.Houses.InvalidFurnitureItem,
                ChatMessageType.Error,
                CustomColors.Alerts.Error
            );
            return false;
        }

        if (!ItemDescriptor.TryGet(inventorySlot.ItemId, out var itemDescriptor))
        {
            return false;
        }

        // Check if item can be used as furniture
        if (!itemDescriptor.CanBeFurniture)
        {
            PacketSender.SendChatMsg(
                _player,
                Strings.Houses.CannotUseFurniture,
                ChatMessageType.Error,
                CustomColors.Alerts.Error
            );
            return false;
        }

        // Find an empty furniture slot
        lock (_lock)
        {
            var emptySlot = -1;
            for (var i = 0; i < _furniture.Count; i++)
            {
                if (_furniture[i].ItemId == Guid.Empty)
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot == -1)
            {
                PacketSender.SendChatMsg(
                    _player,
                    Strings.Houses.NoFurnitureSpace,
                    ChatMessageType.Error,
                    CustomColors.Alerts.Error
                );
                return false;
            }

            // Place the furniture
            _furniture[emptySlot].ItemId = inventorySlot.ItemId;
            _furniture[emptySlot].Quantity = 1;
            _furniture[emptySlot].X = x;
            _furniture[emptySlot].Y = y;
            _furniture[emptySlot].Direction = direction;
            _furniture[emptySlot].Properties = inventorySlot.Properties;

            // Remove from inventory
            if (itemDescriptor.Stackable && inventorySlot.Quantity > 1)
            {
                inventorySlot.Quantity--;
            }
            else
            {
                inventorySlot.Set(Item.None);
            }

            PacketSender.SendInventoryItemUpdate(_player, inventorySlotIndex);
            SendFurnitureUpdate(emptySlot);

            DbInterface.Pool.QueueWorkItem(_house.Save);

            PacketSender.SendChatMsg(
                _player,
                Strings.Houses.FurniturePlaced.ToString(itemDescriptor.Name),
                ChatMessageType.Experience,
                CustomColors.Alerts.Success
            );

            return true;
        }
    }

    public bool TryRemoveFurniture(int furnitureSlotIndex)
    {
        // Check if player has permission to modify furniture
        var permission = _house.GetPermission(_player.Id);
        if (permission != HousePermission.Owner && permission != HousePermission.Modify)
        {
            PacketSender.SendChatMsg(
                _player,
                Strings.Houses.NoPermissionToModify,
                ChatMessageType.Error,
                CustomColors.Alerts.Error
            );
            return false;
        }

        if (furnitureSlotIndex < 0 || furnitureSlotIndex >= _furniture.Count)
        {
            return false;
        }

        var furnitureSlot = _furniture[furnitureSlotIndex];
        if (furnitureSlot == null || furnitureSlot.ItemId == Guid.Empty)
        {
            return false;
        }

        if (!ItemDescriptor.TryGet(furnitureSlot.ItemId, out var itemDescriptor))
        {
            return false;
        }

        lock (_lock)
        {
            // Try to add to inventory
            if (!_player.TryGiveItem(furnitureSlot.ItemId, furnitureSlot.Quantity))
            {
                PacketSender.SendChatMsg(
                    _player,
                    Strings.Houses.InventoryFull,
                    ChatMessageType.Error,
                    CustomColors.Alerts.Error
                );
                return false;
            }

            // Remove from furniture
            furnitureSlot.Set(Item.None);

            SendFurnitureUpdate(furnitureSlotIndex);

            DbInterface.Pool.QueueWorkItem(_house.Save);

            PacketSender.SendChatMsg(
                _player,
                Strings.Houses.FurnitureRemoved.ToString(itemDescriptor.Name),
                ChatMessageType.Experience,
                CustomColors.Alerts.Success
            );

            return true;
        }
    }

    public bool TryMoveFurniture(int furnitureSlotIndex, int x, int y, int direction = 0)
    {
        // Check if player has permission to modify furniture
        var permission = _house.GetPermission(_player.Id);
        if (permission != HousePermission.Owner && permission != HousePermission.Modify)
        {
            PacketSender.SendChatMsg(
                _player,
                Strings.Houses.NoPermissionToModify,
                ChatMessageType.Error,
                CustomColors.Alerts.Error
            );
            return false;
        }

        if (furnitureSlotIndex < 0 || furnitureSlotIndex >= _furniture.Count)
        {
            return false;
        }

        var furnitureSlot = _furniture[furnitureSlotIndex];
        if (furnitureSlot == null || furnitureSlot.ItemId == Guid.Empty)
        {
            return false;
        }

        lock (_lock)
        {
            furnitureSlot.X = x;
            furnitureSlot.Y = y;
            furnitureSlot.Direction = direction;

            SendFurnitureUpdate(furnitureSlotIndex);

            DbInterface.Pool.QueueWorkItem(_house.Save);

            return true;
        }
    }

    public void Dispose()
    {
        SendCloseHouse();
        _player.HouseInterface = null;
    }
}
