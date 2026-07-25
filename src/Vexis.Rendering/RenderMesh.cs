using System.Numerics;

namespace Vexis.Rendering;

public sealed class RenderMesh
{
    public required string Name { get; init; }
    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
    public RenderMaterial Material { get; set; } = RenderMaterial.Default;
    public object? GeometryHandle { get; set; }
}
