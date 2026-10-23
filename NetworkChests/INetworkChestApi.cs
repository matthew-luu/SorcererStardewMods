using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkChest
{
    public interface INetworkChestApi
    {
        bool TryGetInventory(
            StardewValley.Object obj,
            out Inventory inventory
        );

        Item AddItem(
            StardewValley.Object obj,
            Item item
        );
    }
}
