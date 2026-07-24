namespace Vexis.World;

public sealed record AssetRecord(
    Guid Id,
    string Name,
    string Path,
    AssetKind Kind,
    bool IsNew = true,
    DateTimeOffset? LastReimportedAtUtc = null,
    string? SourceExtension = null);

public sealed class AssetImportService
{
    public AssetRecord Import(string path, string? name = null, AssetKind kind = AssetKind.Other)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The asset file does not exist.", path);
        }

        var extension = Path.GetExtension(path);
        return new AssetRecord(
            Guid.NewGuid(),
            name ?? Path.GetFileNameWithoutExtension(path),
            path,
            kind,
            IsNew: true,
            SourceExtension: extension);
    }

    public AssetRecord Reimport(AssetRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!File.Exists(record.Path))
        {
            throw new FileNotFoundException("The asset file does not exist.", record.Path);
        }

        return record with
        {
            IsNew = false,
            LastReimportedAtUtc = DateTimeOffset.UtcNow,
            SourceExtension = string.IsNullOrWhiteSpace(record.SourceExtension) ? Path.GetExtension(record.Path) : record.SourceExtension
        };
    }
}
