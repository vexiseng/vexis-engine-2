using System.Numerics;

namespace Vexis.Rendering;

public sealed record RenderMaterial(string Name, Vector4 BaseColor, float Roughness, float Metallic)
{
    public static RenderMaterial Default { get; } = new("Default", Vector4.One, 0.8f, 0f);
}
