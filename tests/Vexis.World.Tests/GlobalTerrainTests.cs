using Xunit;
using Vexis.World;

namespace Vexis.World.Tests;

public sealed class GlobalTerrainTests
{
    [Fact]
    public void AdjacentRegionsShareTheExactSameBoundaryVertices()
    {
        var field = new GlobalElevationField();
        var leftRegion = new RegionCoordinate(0, 0);
        var rightRegion = new RegionCoordinate(1, 0);

        field.Set(WorldGrid.ToWorldVertex(leftRegion, 64, 12), 137.5f);

        float observedFromRightRegion = field.Get(WorldGrid.ToWorldVertex(rightRegion, 0, 12));
        Assert.Equal(137.5f, observedFromRightRegion);
    }

    [Fact]
    public void NegativeWorldCoordinatesUseFloorDivision()
    {
        Assert.Equal(new RegionCoordinate(-1, -1), WorldGrid.RegionContaining(new WorldCell(-1, -1)));
        Assert.Equal(new RegionCoordinate(-2, 0), WorldGrid.RegionContaining(new WorldCell(-65, 0)));
    }
}
