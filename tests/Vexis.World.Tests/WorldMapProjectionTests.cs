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
}
