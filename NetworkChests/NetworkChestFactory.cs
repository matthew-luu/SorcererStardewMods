
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;
using Pathoschild.Stardew.Automate;

namespace NetworkChest;

internal class NetworkChestFactory : IAutomationFactory
{
    public IAutomatable? GetFor(
        SObject obj,
        GameLocation location,
        in Vector2 tile
    )
    {
        if (obj.QualifiedItemId != NetworkChest.QualifiedItemId)
            return null;

        string? networkId = NetworkChest.GetNetworkId(obj);

        if (string.IsNullOrWhiteSpace(networkId))
            return null;

        return new NetworkChestContainer(networkId, location, tile);
    }

    public IAutomatable? GetFor(
        TerrainFeature feature,
        GameLocation location,
        in Vector2 tile
    )
    {
        return null;
    }

    public IAutomatable? GetFor(
        Building building,
        GameLocation location,
        in Vector2 tile
    )
    {
        return null;
    }

    public IAutomatable? GetForTile(
        GameLocation location,
        in Vector2 tile
    )
    {
        return null;
    }
}
