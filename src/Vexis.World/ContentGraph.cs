namespace Vexis.World;

public sealed record ContentReference(string SourceId, string TargetId, string Kind);

public sealed class ContentGraphService
{
    public IReadOnlyList<ContentReference> BuildReferences(IEnumerable<ValidationContentItem> content)
    {
        var items = content.ToList();
        var references = new List<ContentReference>();

        foreach (var item in items.Where(i => i.Type == "quest"))
        {
            references.Add(new ContentReference(item.Id, item.Id, "self"));
        }

        foreach (var item in items.Where(i => i.Type == "dialogue"))
        {
            references.Add(new ContentReference(item.Id, item.Id, "dialogue"));
        }

        return references;
    }
}
