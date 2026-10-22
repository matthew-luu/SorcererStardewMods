using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace NetworkChest
{
    internal class NetworkChest
    {
        public const int Capacity = 36;

        public const string ItemId =
            "NetworkChest";

        public const string QualifiedItemId = 
            "(BC)" + ItemId;

        public const string NetworkIdKey =
            "NetworkChest/NetworkId";

        public const string NetworkIdsKey = 
            "NetworkChest/NetworkIds";

        private const string InventoryPrefix =
            "NetworkChest/";

        /// <summary>Get the network ID stored on a placed chest.</summary>
        public static string? GetNetworkId(StardewValley.Object chest)
        {
            if (!chest.modData.TryGetValue(NetworkIdKey, out string? networkId)
                || string.IsNullOrWhiteSpace(networkId))
            {
                return null;
            }

            return networkId;
        }

        /// <summary>Set the network ID stored on a placed chest.</summary>
        public static void SetNetworkId(
            StardewValley.Object chest,
            string networkId
        )
        {
            chest.modData[NetworkIdKey] = networkId.Trim();
        }

        /// <summary>Get the shared inventory for a network ID.</summary>
        public static Inventory GetInventory(string networkId)
        {
            return Game1.player.team.GetOrCreateGlobalInventory(
                InventoryPrefix + networkId
            );
        }


        public static ItemGrabMenu.behaviorOnItemSelect CreateDepositHandler(
            string networkId
        )
        {
            return (item, player) =>
            {
                if (item is null)
                    return;

                if (item.Stack == 0)
                    item.Stack = 1;

                Item? remaining = AddItem(networkId, item);

                if (remaining is null)
                {
                    player.removeItemFromInventory(item);
                }
                else
                {
                    remaining = player.addItemToInventory(remaining);
                }

                int oldId = Game1.activeClickableMenu?.currentlySnappedComponent?.myID ?? -1;

                var menu = new ItemGrabMenu(
                    GetInventory(networkId),
                    reverseGrab: false,
                    showReceivingMenu: true,
                    InventoryMenu.highlightAllItems,
                    CreateDepositHandler(networkId),
                    message: $"Network: {networkId}",
                    behaviorOnItemGrab: CreateWithdrawHandler(networkId),
                    canBeExitedWithKey: true,
                    showOrganizeButton: true
                );

                menu.heldItem = remaining;
                Game1.activeClickableMenu = menu;

                if (oldId != -1)
                {
                    menu.currentlySnappedComponent = menu.getComponentWithID(oldId);
                    menu.snapCursorToCurrentSnappedComponent();
                }
            };
        }


        public static ItemGrabMenu.behaviorOnItemSelect CreateWithdrawHandler(
            string networkId
        )
        {
            return (item, player) =>
            {
                if (item is null || !player.couldInventoryAcceptThisItem(item))
                    return;

                Inventory inventory = GetInventory(networkId);
                inventory.Remove(item);

                int oldId = Game1.activeClickableMenu?.currentlySnappedComponent?.myID ?? -1;

                var menu = new ItemGrabMenu(
                    inventory,
                    reverseGrab: false,
                    showReceivingMenu: true,
                    InventoryMenu.highlightAllItems,
                    CreateDepositHandler(networkId),
                    message: $"Network: {networkId}",
                    behaviorOnItemGrab: CreateWithdrawHandler(networkId),
                    canBeExitedWithKey: true,
                    showOrganizeButton: true
                );

                Game1.activeClickableMenu = menu;

                if (oldId != -1)
                {
                    menu.currentlySnappedComponent = menu.getComponentWithID(oldId);
                    menu.snapCursorToCurrentSnappedComponent();
                }
            };
        }

        public static Item? AddItem(string networkId, Item item)
        {
            item.resetState();

            ClearNulls(networkId);

            Inventory inventory = GetInventory(networkId);

            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] != null && inventory[i].canStackWith(item))
                {
                    int toRemove = item.Stack - inventory[i].addToStack(item);

                    if (item.ConsumeStack(toRemove) == null)
                        return null;
                }
            }

            if (inventory.Count < Capacity)
            {
                inventory.Add(item);
                return null;
            }

            return item;
        }

        public static void GrabItemFromInventory(
            string networkId,
            Item item,
            Farmer who
        )
        {
            if (item.Stack == 0)
                item.Stack = 1;

            Item? remaining = AddItem(networkId, item);

            if (remaining is null)
                who.removeItemFromInventory(item);
            else
                remaining = who.addItemToInventory(remaining);

            // The caller must refresh the menu and restore its held item.
        }

        public static void GrabItemFromChest(
            string networkId,
            Item item,
            Farmer who
        )
        {
            if (!who.couldInventoryAcceptThisItem(item))
                return;

            Inventory inventory = GetInventory(networkId);
            inventory.Remove(item);
        }

        public static void ClearNulls(string networkId)
        {
            Inventory inventory = GetInventory(networkId);

            if (!inventory.HasEmptySlots())
                return;

            for (int i = inventory.Count - 1; i >= 0; i--)
            {
                if (inventory[i] is null)
                    inventory.RemoveAt(i);
            }
        }

        public static void RegisterNetworkId(string networkId)
        {
            var data = Game1.player.modData;

            var ids = data.TryGetValue(NetworkIdsKey, out string? value)
                ? value.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            if (!ids.Contains(networkId, StringComparer.OrdinalIgnoreCase))
            {
                ids.Add(networkId);
                data[NetworkIdsKey] = string.Join("|", ids);
            }
        }
    }
}
