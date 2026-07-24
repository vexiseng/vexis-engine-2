using Xunit;
using Vexis.World;

namespace Vexis.World.Tests;

public sealed class RuntimeBundleTests
{
    [Fact]
    public void RuntimeBundleServiceCreatesManifestAndOutputDirectory()
    {
        var tempAsset = Path.Combine(Path.GetTempPath(), $"bundle-{Guid.NewGuid():N}.glb");
        File.WriteAllText(tempAsset, "dummy");

        try
        {
            var service = new RuntimeBundleService();
            var result = service.CreateBundle("Vaelor", [new AssetRecord(Guid.NewGuid(), "Town Model", tempAsset, AssetKind.Model)], ["quest:first_steps"]);

            Assert.True(Directory.Exists(result.OutputDirectory));
            Assert.True(File.Exists(result.ManifestPath));
            Assert.Contains("Vaelor", result.ManifestPath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempAsset);
            if (Directory.Exists(Path.Combine(Path.GetTempPath(), "vexis-runtime-bundles")))
            {
                Directory.Delete(Path.Combine(Path.GetTempPath(), "vexis-runtime-bundles"), recursive: true);
            }
        }
    }
}
