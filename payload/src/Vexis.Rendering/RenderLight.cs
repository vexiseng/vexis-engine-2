using System.Numerics;

namespace Vexis.Rendering;

public sealed record RenderLight(Vector3 Direction, Vector3 Color, float Intensity)
{
    public static RenderLight Sun { get; } = new(
        Vector3.Normalize(new Vector3(-0.35f, -1f, -0.2f)),
        Vector3.One,
        1f);
}
