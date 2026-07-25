namespace Vexis.Rendering;

public sealed class RenderScene
{
    private readonly List<RenderMesh> _meshes = [];
    private readonly List<RenderLight> _lights = [];

    public RenderCamera Camera { get; } = new();
    public IReadOnlyList<RenderMesh> Meshes => _meshes;
    public IReadOnlyList<RenderLight> Lights => _lights;

    public void AddMesh(RenderMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        _meshes.Add(mesh);
    }

    public void AddLight(RenderLight light)
    {
        ArgumentNullException.ThrowIfNull(light);
        _lights.Add(light);
    }

    public void Clear()
    {
        _meshes.Clear();
        _lights.Clear();
    }
}
