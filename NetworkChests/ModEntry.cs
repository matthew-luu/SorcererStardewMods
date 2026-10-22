using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using Pathoschild.Stardew.Automate;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.Menus;

namespace NetworkChest;

internal class ModEntry : Mod
{

    private const string NetworkChestTexture =
        "Sorcerer.Stardew.NetworkChest/assets/NetworkChest";

    private string? ActiveNetworkChestId = null;

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
    }
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var api = this.Helper.ModRegistry
            .GetApi<IAutomateAPI>("Pathoschild.Automate");

        if (api is null)
        {
            this.Monitor.Log(
                "Automate's API wasn't available. Network Chests can't be automated.",
                LogLevel.Error
            );
            return;
        }

        api.AddFactory(new NetworkChestFactory());

        this.Monitor.Log(
            "Registered Network Chest automation factory with Automate.",
            LogLevel.Info
        );
    }
    private void OnAssetRequested(
        object? sender,
        AssetRequestedEventArgs e
    )
    {
        // Add the Network Chest.
        if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(asset =>
            {
                IDictionary<string, BigCraftableData> data =
                    asset.AsDictionary<string, BigCraftableData>().Data;

                data[NetworkChest.ItemId] = new BigCraftableData
                {
                    Name = "Network Chest",
                    DisplayName = "Network Chest",
                    Description = "A chest designed to connect to a shared storage network.",
                    Texture = NetworkChestTexture,
                    SpriteIndex = 0,
                    CanBePlacedIndoors = true,
                    CanBePlacedOutdoors = true
                };
            });
        }

        // Add the Network Chest crafting recipe.
        if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(asset =>
            {
                IDictionary<string, string> recipes =
                    asset.AsDictionary<string, string>().Data;

                recipes["Network Chest"] =
                    "(BC)256 1 (BC)239 1/Home/" + NetworkChest.ItemId + "/true/default/";
            });
        }

        // Load the Network Chest's custom texture.
        if (e.NameWithoutLocale.IsEquivalentTo(NetworkChestTexture))
        {
            e.LoadFromModFile<Texture2D>(
                "assets/network-chest.png",
                AssetLoadPriority.Medium
            );
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady
            || e.Button != SButton.MouseRight
            || Game1.activeClickableMenu != null)
        {
            return;
        }

        Vector2 tile = e.Cursor.GrabTile;
        GameLocation location = Game1.currentLocation;

        if (!location.objects.TryGetValue(tile, out StardewValley.Object? obj))
            return;

        if (obj.QualifiedItemId != NetworkChest.QualifiedItemId)
            return;

        this.Helper.Input.Suppress(e.Button);

        string? networkId = NetworkChest.GetNetworkId(obj);

        if (networkId is null)
        {
            this.OpenNetworkSelectionMenu(obj, location, tile);
        }
        else
        {
            this.OpenNetworkChest(networkId);
        }
    }


    private List<string> GetNetworksWithItems()
    {
        // Migration: discover IDs from placed Network Chests.
        foreach (GameLocation location in Game1.locations)
        {
            foreach (StardewValley.Object obj in location.objects.Values)
            {
                if (obj.QualifiedItemId != NetworkChest.QualifiedItemId)
                    continue;

                string? id = NetworkChest.GetNetworkId(obj);

                if (!string.IsNullOrWhiteSpace(id))
                    NetworkChest.RegisterNetworkId(id.Trim());
            }
        }

        // Read all registered IDs, including networks with no placed chest.
        if (!Game1.player.modData.TryGetValue(
                NetworkChest.NetworkIdsKey,
                out string? value))
        {
            return new List<string>();
        }

        return value
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Where(id => NetworkChest.GetInventory(id)
                .Any(item => item is { Stack: > 0 }))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }



    private void OpenNetworkSelectionMenu(
        StardewValley.Object chest,
        GameLocation location,
        Microsoft.Xna.Framework.Vector2 tile)
    {
        List<string> networks = this.GetNetworksWithItems();

        // Build the selectable entries. Stardew handles pagination.
        var responses = new List<KeyValuePair<string, string>>
        {
            new("network_add", "Add a new Network ID")
        };

        foreach (string id in networks)
        {
            responses.Add(new KeyValuePair<string, string>(
                $"network_select_{id}",
                id
            ));
        }

        location.ShowPagedResponses(
            "Select a Network ID:",
            responses,
            answer =>
            {
                if (answer == "network_add")
                {
                    Game1.activeClickableMenu = new NamingMenu(
                        value =>
                        {
                            if (string.IsNullOrWhiteSpace(value))
                                return;

                            string newId = value.Trim();

                            NetworkChest.RegisterNetworkId(newId);
                            NetworkChest.SetNetworkId(chest, newId);

                            this.SendNetworkChestUpdate(location, tile);

                            this.Monitor.Log(
                                $"Network Chest assigned to network '{newId}'.",
                                LogLevel.Info
                            );

                            this.OpenNetworkChest(newId);
                        },
                        "Enter a Network ID:"
                    );

                    return;
                }

                if (answer.StartsWith("network_select_", StringComparison.Ordinal))
                {
                    string id = answer["network_select_".Length..];

                    NetworkChest.RegisterNetworkId(id);
                    NetworkChest.SetNetworkId(chest, id);

                    this.SendNetworkChestUpdate(location, tile);

                    this.Monitor.Log(
                        $"Network Chest assigned to network '{id}'.",
                        LogLevel.Info
                    );

                    this.OpenNetworkChest(id);
                }
            },
            auto_select_single_choice: false,
            addCancel: true,
            itemsPerPage: 5
        );
    }


    private void SendNetworkChestUpdate(
    GameLocation location,
    Microsoft.Xna.Framework.Vector2 tile)
    {
        this.Monitor.Log(
            $"Sending AutomateUpdateChestMessage for {location.Name} at {tile}.",
            LogLevel.Info
        );

        this.Helper.Multiplayer.SendMessage(
            new
            {
                LocationName = location.Name,
                Tile = tile
            },
            "AutomateUpdateChestMessage",
            modIDs: new[] { "Pathoschild.Automate" }
        );
    }


    private void OpenNetworkChest(string networkId)
    {
        var inventory = NetworkChest.GetInventory(networkId);

        this.ActiveNetworkChestId = networkId;

        Game1.activeClickableMenu = new ItemGrabMenu(
            inventory,
            reverseGrab: false,
            showReceivingMenu: true,
            InventoryMenu.highlightAllItems,
            NetworkChest.CreateDepositHandler(networkId),
            message: null,
            behaviorOnItemGrab: NetworkChest.CreateWithdrawHandler(networkId),
            canBeExitedWithKey: true,
            showOrganizeButton: true
        );
    }

    private void OnRenderedActiveMenu(
    object? sender,
    RenderedActiveMenuEventArgs e)
    {
        if (this.ActiveNetworkChestId is not string networkId)
            return;

        if (Game1.activeClickableMenu is not ItemGrabMenu menu)
        {
            this.ActiveNetworkChestId = null;
            return;
        }

        // The receiving inventory is the upper inventory in the vanilla menu.
        var storageMenu = menu.ItemsToGrabMenu;

        string label = $"Network: {networkId}";
        Vector2 textSize = Game1.dialogueFont.MeasureString(label);

        float x = storageMenu.xPositionOnScreen
            + (storageMenu.width - textSize.X) / 2f;

        float y = storageMenu.yPositionOnScreen - textSize.Y - 12f;

        // Draw a dark shadow for readability.
        e.SpriteBatch.DrawString(
            Game1.dialogueFont,
            label,
            new Vector2(x + 2f, y + 2f),
            Color.Black * 0.9f
        );

        e.SpriteBatch.DrawString(
            Game1.dialogueFont,
            label,
            new Vector2(x, y),
            Color.White
        );
    }

}
