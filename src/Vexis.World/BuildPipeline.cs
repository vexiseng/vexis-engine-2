namespace Vexis.World;

public sealed record BuildTask(string Name, string Status);

public sealed class BuildPipelineService
{
    public IReadOnlyList<BuildTask> CreatePlan(
        IEnumerable<ValidationIssue> issues,
        IEnumerable<AssetRecord> assets,
        bool hasTerrain = true,
        bool hasContent = true)
    {
        var issueList = issues.ToList();
        var assetList = assets.ToList();
        var hasBlockers = issueList.Count > 0;
        var canPackage = hasTerrain && assetList.Count > 0;
        var canLaunch = hasTerrain && hasContent && assetList.Count > 0 && !hasBlockers;

        return
        [
            new BuildTask("Validate world data", hasBlockers ? "Needs attention" : "Ready"),
            new BuildTask("Package runtime bundle", canPackage ? "Ready" : "Pending"),
            new BuildTask("Launch runtime", canLaunch ? "Queued" : "Blocked")
        ];
    }
}
