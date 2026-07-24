namespace Vexis.World;

public sealed record ValidationIssue(string Code, string Message);

public sealed record ValidationContentItem(string Type, string Id);

public sealed record ValidationSceneObject(string Name, float X, float Z);

public enum AssetKind
{
    Model,
    Texture,
    Audio,
    Animation,
    Material,
    Other
}

public sealed record ValidationAsset(string Name, string Path, AssetKind Kind);

public sealed class ProjectValidationService
{
    public IReadOnlyList<ValidationIssue> Validate(
        int terrainWidth,
        int terrainHeight,
        IEnumerable<ValidationSceneObject> objects,
        IEnumerable<ValidationContentItem> content,
        IEnumerable<ValidationAsset>? assets = null)
    {
        var issues = new List<ValidationIssue>();

        if (terrainWidth <= 0 || terrainHeight <= 0)
        {
            issues.Add(new ValidationIssue("invalid-terrain-size", "Terrain dimensions must be positive."));
        }

        var seenContentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ValidationContentItem item in content)
        {
            if (!seenContentIds.Add(item.Id))
            {
                issues.Add(new ValidationIssue("duplicate-content-id", $"Content ID '{item.Id}' is duplicated."));
            }
        }

        foreach (ValidationSceneObject obj in objects)
        {
            if (obj.X < 0 || obj.X >= terrainWidth || obj.Z < 0 || obj.Z >= terrainHeight)
            {
                issues.Add(new ValidationIssue("object-out-of-bounds", $"Object '{obj.Name}' falls outside the terrain bounds."));
            }
        }

        foreach (ValidationAsset asset in assets ?? [])
        {
            if (!File.Exists(asset.Path))
            {
                issues.Add(new ValidationIssue("missing-asset-file", $"Asset '{asset.Name}' points to missing file '{asset.Path}'."));
            }
        }

        return issues;
    }
}
