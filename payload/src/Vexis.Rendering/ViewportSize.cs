namespace Vexis.Rendering;

public readonly record struct ViewportSize(int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
    public float AspectRatio => IsEmpty ? 1f : Width / (float)Height;

    public ViewportSize ClampToValid() => new(Math.Max(1, Width), Math.Max(1, Height));
}
