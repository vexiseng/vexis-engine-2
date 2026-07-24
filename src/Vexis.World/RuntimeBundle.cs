namespace Vexis.World;

public sealed record RuntimeBundleResult(string OutputDirectory, string ManifestPath);

public sealed class RuntimeBundleService
{
    public RuntimeBundleResult CreateBundle(string projectName, IEnumerable<AssetRecord> assets, IEnumerable<string> contentIds)
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), "vexis-runtime-bundles");
        var outputDir = Path.Combine(outputRoot, projectName.Replace(" ", "-", StringComparison.OrdinalIgnoreCase));
        Directory.CreateDirectory(outputDir);

        var manifestPath = Path.Combine(outputDir, "bundle-manifest.json");
        var manifest = new
        {
            projectName,
            generatedAtUtc = DateTimeOffset.UtcNow,
            assetCount = assets.Count(),
            contentIds = contentIds.ToList()
        };

        File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        return new RuntimeBundleResult(outputDir, manifestPath);
    }
}
