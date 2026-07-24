using Xunit;
using Vexis.World;

namespace Vexis.World.Tests;

public sealed class ProjectPipelineTests
{
    [Fact]
    public void BuildPipelineCreatesPackagingAndLaunchTasksWhenProjectLooksHealthy()
    {
        var service = new BuildPipelineService();
        var plan = service.CreatePlan([], [new AssetRecord(Guid.NewGuid(), "Town Model", "assets/town.glb", AssetKind.Model)]);

        Assert.Contains(plan, task => task.Name == "Package runtime bundle" && task.Status == "Ready");
        Assert.Contains(plan, task => task.Name == "Launch runtime" && task.Status == "Queued");
    }

    [Fact]
    public void RuntimeLaunchServicePromotesReadyWhenTerrainAssetsAndContentExistAndNoIssues()
    {
        var service = new RuntimeLaunchService();
        var state = service.CreateState(true, true, true, []);

        Assert.Equal("Ready", state.Status);
        Assert.Contains("runtime bundle", state.Message, StringComparison.OrdinalIgnoreCase);
    }
}
