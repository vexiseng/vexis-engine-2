namespace Vexis.World;

public enum WorldMapIconCategory
{
    Settlement,
    Dungeon,
    Quest,
    Transport,
    Resource,
    Activity,
    Custom
}

public sealed record WorldMapPointOfInterest(
    Guid Id,
    string Name,
    double WorldX,
    double WorldZ,
    WorldMapIconCategory Category,
    string IconAsset,
    string? Tooltip = null);

public sealed record WorldMapBounds(double MinX, double MinZ, double MaxX, double MaxZ)
{
    public double Width => MaxX - MinX;
    public double Height => MaxZ - MinZ;
}

public readonly record struct MapPixel(double X, double Y);

/// <summary>
/// Stable world-to-map projection shared by the editor, game client, tile baker,
/// markers, paths, fog of war, and click-to-coordinate conversion.
/// </summary>
public sealed class WorldMapProjection(WorldMapBounds bounds, int baseWidth, int baseHeight)
{
    public MapPixel WorldToPixel(double worldX, double worldZ)
    {
        ValidateBounds();
        double normalizedX = (worldX - bounds.MinX) / bounds.Width;
        double normalizedZ = (worldZ - bounds.MinZ) / bounds.Height;
        return new MapPixel(normalizedX * baseWidth, (1d - normalizedZ) * baseHeight);
    }

    public (double WorldX, double WorldZ) PixelToWorld(double pixelX, double pixelY)
    {
        ValidateBounds();
        double normalizedX = pixelX / baseWidth;
        double normalizedZ = 1d - (pixelY / baseHeight);
        return (
            bounds.MinX + (normalizedX * bounds.Width),
            bounds.MinZ + (normalizedZ * bounds.Height));
    }

    public (int TileX, int TileY) PixelToTile(MapPixel pixel, int zoom, int tileSize = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(zoom);
        int scale = checked(1 << zoom);
        double scaledX = pixel.X * scale;
        double scaledY = pixel.Y * scale;
        return ((int)Math.Floor(scaledX / tileSize), (int)Math.Floor(scaledY / tileSize));
    }

    private void ValidateBounds()
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || baseWidth <= 0 || baseHeight <= 0)
        {
            throw new InvalidOperationException("World-map bounds and image dimensions must be positive.");
        }
    }
}

public sealed record WorldMapBakeRequest(
    WorldMapBounds Bounds,
    int BaseWidth,
    int BaseHeight,
    int MaximumZoom,
    IReadOnlyCollection<RegionCoordinate> DirtyRegions);

public sealed record WorldMapLayerSet(
    bool Terrain = true,
    bool Water = true,
    bool Roads = true,
    bool Buildings = true,
    bool Labels = true,
    bool PointsOfInterest = true,
    bool FogOfWar = false);
