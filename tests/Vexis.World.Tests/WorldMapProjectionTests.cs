using Xunit;
using Vexis.World;

namespace Vexis.World.Tests;

public sealed class WorldMapProjectionTests
{
    [Fact]
    public void ProjectionRoundTripsWorldCoordinates()
    {
        var projection = new WorldMapProjection(new WorldMapBounds(-100, -200, 900, 800), 4096, 4096);
        MapPixel pixel = projection.WorldToPixel(250, 125);
        (double x, double z) = projection.PixelToWorld(pixel.X, pixel.Y);

        Assert.Equal(250, x, 8);
        Assert.Equal(125, z, 8);
    }

    [Fact]
    public void TileProjectionRoundTripsWorldCoordinates()
    {
        var projection = new WorldMapProjection(new WorldMapBounds(-100, -200, 900, 800), 2048, 2048);
        var pixel = projection.WorldToPixel(250, 125);
        (int tileX, int tileY) = projection.PixelToTile(pixel, 2, 256);
        (double x, double z) = projection.TileToWorld(tileX, tileY, 2, 256);
        var originPixel = projection.WorldToPixel(x, z);
        (int roundTripTileX, int roundTripTileY) = projection.PixelToTile(originPixel, 2, 256);

        Assert.Equal(11, tileX);
        Assert.Equal(21, tileY);
        Assert.Equal(tileX, roundTripTileX);
        Assert.Equal(tileY, roundTripTileY);
    }
}
