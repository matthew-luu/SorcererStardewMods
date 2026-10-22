
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Mods;
using StardewValley.Objects;
using Pathoschild.Stardew.Automate;

namespace NetworkChest;

internal class NetworkChestContainer : IContainer
{
    private readonly string NetworkId;
    private readonly ModDataDictionary ModDataValues = new();

    public GameLocation Location { get; }

    public Rectangle TileArea { get; }

    public Inventory Inventory =>
        NetworkChest.GetInventory(this.NetworkId);

    IInventory IContainer.Inventory => this.Inventory;

    public string TypeId => NetworkChest.QualifiedItemId;

    public string Name => $"Network Chest ({this.NetworkId})";

    public ModDataDictionary ModData => this.ModDataValues;

    public bool IsJunimoChest => false;

    public bool IsLocked => false;

    public object InventoryReferenceId => this.Inventory;

    public NetworkChestContainer(
        string networkId,
        GameLocation location,
        Vector2 tile
    )
    {
        this.NetworkId = networkId;
        this.Location = location;
        this.TileArea = new Rectangle(
            (int)tile.X,
            (int)tile.Y,
            1,
            1
        );
    }

    public void Store(ITrackedStack stack)
    {
        if (stack.Count <= 0)
            return;

        NetworkChest.ClearNulls(this.NetworkId);

        Inventory inventory = this.Inventory;

        foreach (Item? slot in inventory)
        {
            if (slot is null || !stack.Sample.canStackWith(slot))
                continue;

            Item sample = stack.Sample.getOne();
            sample.Stack = stack.Count;

            int added = stack.Count - slot.addToStack(sample);
            stack.Reduce(added);

            if (stack.Count <= 0)
                return;
        }

        if (inventory.Count < NetworkChest.Capacity)
            inventory.Add(stack.Take(stack.Count));
    }

    public int GetFilled()
    {
        return this.Inventory.Count(item => item is { Stack: > 0 });
    }

    public int GetCapacity()
    {
        return NetworkChest.Capacity;
    }

    public ITrackedStack? Get(Func<Item, bool> predicate, int count)
    {
        if (count <= 0)
            return null;

        var stacks = new List<ITrackedStack>();
        int countFound = 0;

        foreach (Item? item in this.Inventory.ToArray())
        {
            if (item is not { Stack: > 0 } || !predicate(item))
                continue;

            ITrackedStack tracked = new TrackedItem(item)
                .OnEmpty((_, taken) => this.Inventory.Remove(taken));

            stacks.Add(tracked);
            countFound += item.Stack;

            if (countFound >= count)
                break;
        }

        return stacks.Count > 0
            ? new TrackedItemCollection(stacks.ToArray())
            : null;
    }

    public IEnumerator<ITrackedStack> GetEnumerator()
    {
        foreach (Item? item in this.Inventory.ToArray())
        {
            if (item is not { Stack: > 0 })
                continue;

            yield return new TrackedItem(item)
                .OnEmpty((_, taken) => this.Inventory.Remove(taken));
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
