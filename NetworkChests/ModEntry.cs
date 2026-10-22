
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.Menus;
using Pathoschild.Stardew.Automate;

namespace NetworkChest;

internal class ModEntry : Mod
{

    private const string NetworkChestTexture =
        "Sorcerer.Stardew.NetworkChest/assets/NetworkChest";

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
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
            Game1.activeClickableMenu = new NamingMenu(
                value =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return;

                    string savedId = value.Trim();
                    NetworkChest.SetNetworkId(obj, savedId);

                    this.Monitor.Log(
                        $"Network Chest assigned to network '{savedId}'.",
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

                    this.OpenNetworkChest(savedId);
                },
                "Enter a Network ID:"
            );
        }
        else
        {
            this.OpenNetworkChest(networkId);
        }
    }

    private void OpenNetworkChest(string networkId)
    {
        var inventory = NetworkChest.GetInventory(networkId);

        Game1.activeClickableMenu = new ItemGrabMenu(
            inventory,
            reverseGrab: false,
            showReceivingMenu: true,
            InventoryMenu.highlightAllItems,
            NetworkChest.CreateDepositHandler(networkId),
            message: $"Network: {networkId}",
            behaviorOnItemGrab: NetworkChest.CreateWithdrawHandler(networkId),
            canBeExitedWithKey: true,
            showOrganizeButton: true
        );
    }
}
