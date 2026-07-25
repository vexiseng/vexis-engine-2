using System.Numerics;

namespace Vexis.Rendering;

public readonly record struct RenderFrameContext(
    TimeSpan TotalTime,
    TimeSpan DeltaTime,
    ViewportSize Viewport,
    Vector4 ClearColor)
{
    public static RenderFrameContext Default(ViewportSize viewport) =>
        new(TimeSpan.Zero, TimeSpan.Zero, viewport, new Vector4(0.055f, 0.075f, 0.095f, 1f));
}
