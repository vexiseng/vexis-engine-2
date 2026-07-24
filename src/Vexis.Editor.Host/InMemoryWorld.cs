namespace Vexis.Editor.Host;

public sealed record WorldObject(Guid Id, string Asset, float X, float Y, float Z);

public sealed class InMemoryWorld
{
    private readonly List<WorldObject> _objects = [];
    public IReadOnlyList<WorldObject> Objects => _objects;

    public WorldObject Add(string asset, float x, float y, float z)
    {
        var item = new WorldObject(Guid.NewGuid(), asset, x, y, z);
        _objects.Add(item);
        return item;
    }

    public bool Remove(Guid id)
    {
        var index = _objects.FindIndex(item => item.Id == id);
        if (index < 0)
        {
            return false;
        }

        _objects.RemoveAt(index);
        return true;
    }
}
