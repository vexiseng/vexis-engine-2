using Xunit;
using System.Numerics;
using Vexis.Rendering;

namespace Vexis.Rendering.Tests;

public sealed class RenderCameraTests
{
    [Fact]
    public void Camera_vectors_are_finite_and_orthogonal()
    {
        var camera = new RenderCamera();

        Assert.True(IsFinite(camera.Forward));
        Assert.True(IsFinite(camera.Right));
        Assert.True(IsFinite(camera.Up));
        Assert.InRange(MathF.Abs(Vector3.Dot(camera.Forward, camera.Right)), 0f, 0.0001f);
        Assert.InRange(MathF.Abs(Vector3.Dot(camera.Forward, camera.Up)), 0f, 0.0001f);
    }

    [Fact]
    public void Projection_uses_safe_aspect_ratio_for_empty_viewport()
    {
        var camera = new RenderCamera();
        var projection = camera.CreateProjectionMatrix(new ViewportSize(0, 0));

        Assert.True(IsFinite(projection));
    }

    [Fact]
    public void Look_clamps_pitch()
    {
        var camera = new RenderCamera();
        camera.Look(0f, 100f);

        Assert.InRange(camera.Pitch, -1.553343f, 1.553343f);
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static bool IsFinite(Matrix4x4 value) =>
        float.IsFinite(value.M11) && float.IsFinite(value.M12) && float.IsFinite(value.M13) && float.IsFinite(value.M14) &&
        float.IsFinite(value.M21) && float.IsFinite(value.M22) && float.IsFinite(value.M23) && float.IsFinite(value.M24) &&
        float.IsFinite(value.M31) && float.IsFinite(value.M32) && float.IsFinite(value.M33) && float.IsFinite(value.M34) &&
        float.IsFinite(value.M41) && float.IsFinite(value.M42) && float.IsFinite(value.M43) && float.IsFinite(value.M44);
}
