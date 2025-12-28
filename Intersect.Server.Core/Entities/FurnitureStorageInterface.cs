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
/// Manages storage for furniture containers.
/// Similar to BankInterface but for furniture storage.
/// </summary>
public partial class FurnitureStorageInterface : IDisposable
{
    private readonly Player _player;
    private readonly FurnitureStorage _storage;
    private readonly SlotList<FurnitureStorageSlot> _slots;
    private readonly object _lock = new object();

    public FurnitureStorageInterface(Player player, FurnitureStorage storage, int slotCount)
    {
        _player = player;
        _storage = storage;

        // Initialize slot list
        _slots = new SlotList<FurnitureStorageSlot>(slotCount, FurnitureStorageSlot.Create);

        // Load existing slots from storage
        foreach (var slot in storage.Slots.OrderBy(s => s.Slot))
        {
            if (slot.Slot < slotCount)
            {
                _slots[slot.Slot] = slot;
            }
        }
    }

    public void SendOpenStorage()
    {
        var slotUpdatePackets = new List<FurnitureStorageUpdatePacket>();

        for (var slot = 0; slot < _slots.Capacity; slot++)
        {
            var storageSlot = slot < _slots.Count ? _slots[slot] : default;
            slotUpdatePackets.Add(
                new FurnitureStorageUpdatePacket(
                    slot,
                    storageSlot?.ItemId ?? Guid.Empty,
                    storageSlot?.Quantity ?? 0,
                    storageSlot?.BagId,
                    storageSlot?.Properties
                )
            );
        }

        _player?.SendPacket(
            new FurnitureStoragePacket(
                false,
                _slots.Capacity,
                slotUpdatePackets.ToArray()
            )
        );
    }

    public void SendStorageUpdate(int slot)
    {
        if (_slots[slot] != null && _slots[slot].ItemId != Guid.Empty && _slots[slot].Quantity > 0)
        {
            _player?.SendPacket(
                new FurnitureStorageUpdatePacket(
                    slot,
                    _slots[slot].ItemId,
                    _slots[slot].Quantity,
                    _slots[slot].BagId,
                    _slots[slot].Properties
                )
            );
        }
        else
        {
            _player?.SendPacket(new FurnitureStorageUpdatePacket(slot, Guid.Empty, 0, null, null));
        }
    }

    public void SendCloseStorage()
    {
        _player?.SendPacket(new FurnitureStoragePacket(true, -1, null));
    }

    public bool TryDepositItem(int inventorySlotIndex, int quantityHint, int storageSlotIndex = -1)
    {
        var inventorySlot = _player.Items[inventorySlotIndex];
        if (inventorySlot == null || inventorySlot.ItemId == Guid.Empty)
        {
            PacketSender.SendChatMsg(
                _player,
                Strings.FurnitureStorage.DepositInvalid,
                ChatMessageType.Error,
                CustomColors.Alerts.Error
            );
            return false;
        }

        if (!ItemDescriptor.TryGet(inventorySlot.ItemId, out var itemDescriptor))
        {
            return false;
        }

        lock (_lock)
        {
            // Similar logic to BankInterface deposit
            var sourceSlots = _player.Items.ToArray();
            var maximumStack = itemDescriptor.Stackable ? itemDescriptor.MaxInventoryStack : 1;
            var sourceQuantity = Item.FindQuantityOfItem(itemDescriptor.Id, sourceSlots);

            _slots.FillToCapacity();
            var targetSlots = _slots.ToArray();

            var movableQuantity = Item.FindSpaceForItem(
                itemDescriptor.Id,
                itemDescriptor.ItemType,
                maximumStack,
                storageSlotIndex,
                quantityHint < 0 ? sourceQuantity : quantityHint,
                targetSlots
            );

            if (movableQuantity <= 0)
            {
                PacketSender.SendChatMsg(
                    _player,
                    Strings.FurnitureStorage.NoSpace,
                    ChatMessageType.Error,
                    CustomColors.Alerts.Error
                );
                return false;
            }

            // Remove from inventory
            _player.TryTakeItem(itemDescriptor.Id, movableQuantity);

            // Add to storage
            var slotIndicesToFill = Item.FindCompatibleSlotsForItem(
                itemDescriptor.Id,
                itemDescriptor.ItemType,
                maximumStack,
                storageSlotIndex,
                movableQuantity,
                targetSlots
            );

            var remainingQuantity = movableQuantity;
            foreach (var slotIndexToFill in slotIndicesToFill)
            {
                if (remainingQuantity <= 0) break;

                var slotToFill = targetSlots[slotIndexToFill];
                var quantityToAdd = Math.Min(remainingQuantity, maximumStack - slotToFill.Quantity);

                if (slotToFill.ItemId == default)
                {
                    slotToFill.ItemId = itemDescriptor.Id;
                }

                slotToFill.Quantity += quantityToAdd;
                remainingQuantity -= quantityToAdd;

                SendStorageUpdate(slotIndexToFill);
            }

            // Save to database
            DbInterface.Pool.QueueWorkItem(SaveStorage);

            PacketSender.SendChatMsg(
                _player,
                Strings.FurnitureStorage.DepositSuccess.ToString(movableQuantity, itemDescriptor.Name),
                ChatMessageType.Experience,
                CustomColors.Alerts.Success
            );

            return true;
        }
    }

    public bool TryWithdrawItem(int storageSlotIndex, int quantityHint)
    {
        var storageSlot = _slots[storageSlotIndex];
        if (storageSlot == null || storageSlot.ItemId == Guid.Empty)
        {
            return false;
        }

        if (!ItemDescriptor.TryGet(storageSlot.ItemId, out var itemDescriptor))
        {
            return false;
        }

        lock (_lock)
        {
            var quantityToWithdraw = Math.Min(quantityHint, storageSlot.Quantity);

            if (!_player.TryGiveItem(itemDescriptor.Id, quantityToWithdraw))
            {
                PacketSender.SendChatMsg(
                    _player,
                    Strings.FurnitureStorage.InventoryFull,
                    ChatMessageType.Error,
                    CustomColors.Alerts.Error
                );
                return false;
            }

            storageSlot.Quantity -= quantityToWithdraw;
            if (storageSlot.Quantity <= 0)
            {
                storageSlot.Set(Item.None);
            }

            SendStorageUpdate(storageSlotIndex);

            // Save to database
            DbInterface.Pool.QueueWorkItem(SaveStorage);

            PacketSender.SendChatMsg(
                _player,
                Strings.FurnitureStorage.WithdrawSuccess.ToString(quantityToWithdraw, itemDescriptor.Name),
                ChatMessageType.Experience,
                CustomColors.Alerts.Success
            );

            return true;
        }
    }

    private void SaveStorage()
    {
        lock (_lock)
        {
            using var context = DbInterface.CreatePlayerContext(readOnly: false);
            context.Update(_storage);
            context.ChangeTracker.DetectChanges();
            context.SaveChanges();
        }
    }

    public void Dispose()
    {
        SendCloseStorage();
        SaveStorage();
    }
}
