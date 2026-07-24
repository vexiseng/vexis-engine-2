namespace Vexis.World;

public sealed record RuntimeLaunchState(string Status, string Message);

public sealed class RuntimeLaunchService
{
    public RuntimeLaunchState CreateState(bool hasTerrain, bool hasAssets, bool hasContent, IEnumerable<ValidationIssue>? issues = null)
    {
        var issueList = (issues ?? []).ToList();
        if (issueList.Count > 0)
        {
            return new RuntimeLaunchState("Blocked", $"Validation found {issueList.Count} issue(s) that must be addressed before launch.");
        }

        if (!hasTerrain) return new RuntimeLaunchState("Blocked", "Terrain data is missing.");
        if (!hasAssets) return new RuntimeLaunchState("Queued", "Runtime assets are still being prepared.");
        if (!hasContent) return new RuntimeLaunchState("Queued", "Content database is still being prepared.");
        return new RuntimeLaunchState("Ready", "Runtime bundle is prepared for launch from the active project snapshot.");
    }
}
