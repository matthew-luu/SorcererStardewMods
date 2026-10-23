using StardewValley;
using StardewValley.Inventories;

namespace NetworkChest
{
    public class NetworkChestApi : INetworkChestApi
    {
        public bool TryGetInventory(
            StardewValley.Object obj,
            out Inventory inventory
        )
        {
            inventory = null;

            if (obj == null || obj.QualifiedItemId != NetworkChest.QualifiedItemId)
                return false;

            string networkId = NetworkChest.GetNetworkId(obj);

            if (string.IsNullOrWhiteSpace(networkId))
                return false;

            inventory = NetworkChest.GetInventory(networkId);
            return inventory != null;
        }

        public Item AddItem(
            StardewValley.Object obj,
            Item item
        )
        {
            string? networkId = NetworkChest.GetNetworkId(obj);

            if (string.IsNullOrWhiteSpace(networkId))
                return item;

            return NetworkChest.AddItem(networkId, item);
        }
    }
}
