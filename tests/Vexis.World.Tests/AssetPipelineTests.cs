using Xunit;
using Vexis.World;

namespace Vexis.World.Tests;

public sealed class AssetPipelineTests
{
    [Fact]
    public void ImportServiceCreatesAssetRecordForExistingFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"asset-{Guid.NewGuid():N}.obj");
        File.WriteAllText(tempPath, "dummy");

        try
        {
            var service = new AssetImportService();
            var result = service.Import(tempPath, "Test Model", AssetKind.Model);

            Assert.True(result.IsNew);
            Assert.Equal("Test Model", result.Name);
            Assert.Equal(tempPath, result.Path);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void ReimportServiceMarksAssetAsReimportedAndKeepsIdentity()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"asset-{Guid.NewGuid():N}.glb");
        File.WriteAllText(tempPath, "dummy");

        try
        {
            var service = new AssetImportService();
            var imported = service.Import(tempPath, "Test Model", AssetKind.Model);
            var reimported = service.Reimport(imported);

            Assert.Equal(imported.Id, reimported.Id);
            Assert.False(reimported.IsNew);
            Assert.True(reimported.LastReimportedAtUtc.HasValue);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void ValidationFlagsMissingAssetFiles()
    {
        var service = new ProjectValidationService();
        var issues = service.Validate(
            64,
            64,
            [],
            [],
            [new ValidationAsset("Missing model", "assets/missing.glb", AssetKind.Model)]);

        Assert.Contains(issues, issue => issue.Code == "missing-asset-file");
    }
}
