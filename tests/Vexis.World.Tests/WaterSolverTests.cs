using Xunit;
using Vexis.World;

namespace Vexis.World.Tests;

public sealed class WaterSolverTests
{
    [Fact]
    public void WaterCrossesRegionBoundariesWithoutASeparateStitchStep()
    {
        var terrain = new GlobalElevationField { DefaultElevation = 0f };
        var solver = new WaterBodySolver(terrain);
        var definition = new WaterBodyDefinition(
            Guid.NewGuid(),
            "Test Lake",
            5f,
            [new WorldCell(63, 10)],
            new WaterSolveBounds(62, 9, 66, 11));

        SolvedWaterBody result = solver.Solve(definition);

        Assert.Contains(new WorldCell(63, 10), result.Cells.Keys);
        Assert.Contains(new WorldCell(64, 10), result.Cells.Keys);
        Assert.Contains(new WorldCell(65, 10), result.Cells.Keys);
    }

    [Fact]
    public void RaisedTerrainActsAsAnUnderstandableBarrier()
    {
        var terrain = new GlobalElevationField { DefaultElevation = 0f };
        for (int z = 0; z <= 4; z++)
        {
            terrain.Set(new WorldVertex(2, z), 10f);
            terrain.Set(new WorldVertex(3, z), 10f);
        }

        var solver = new WaterBodySolver(terrain);
        var definition = new WaterBodyDefinition(
            Guid.NewGuid(),
            "Blocked Lake",
            5f,
            [new WorldCell(0, 1)],
            new WaterSolveBounds(0, 0, 4, 3));

        SolvedWaterBody result = solver.Solve(definition);

        Assert.DoesNotContain(new WorldCell(3, 1), result.Cells.Keys);
    }

    [Fact]
    public void SolvedWaterBodyReportsPositiveVolumeAndDepthMetrics()
    {
        var terrain = new GlobalElevationField { DefaultElevation = 0f };
        var solver = new WaterBodySolver(terrain);
        var definition = new WaterBodyDefinition(
            Guid.NewGuid(),
            "Volume Lake",
            4f,
            [new WorldCell(1, 1)],
            new WaterSolveBounds(0, 0, 3, 3));

        SolvedWaterBody result = solver.Solve(definition);

        Assert.True(result.EstimatedVolume > 0f);
        Assert.True(result.AverageDepth > 0f);
    }
}
