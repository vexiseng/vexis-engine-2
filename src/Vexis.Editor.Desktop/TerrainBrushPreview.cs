namespace Vexis.Editor.Desktop;

/// <summary>
/// Shared renderer-independent state for the active terrain brush.
/// </summary>
public sealed class TerrainBrushPreview
{
    public event Action? Changed;

    public float X { get; private set; }
    public float Z { get; private set; }
    public float Radius { get; private set; } = 4f;
    public float Strength { get; private set; } = .22f;
    public float Falloff { get; private set; } = .55f;
    public TerrainBrushMode Mode { get; private set; } = TerrainBrushMode.Raise;
    public bool IsVisible { get; private set; }

    public void UpdatePosition(float x, float z, bool visible)
    {
        if (NearlyEqual(X, x) && NearlyEqual(Z, z) && IsVisible == visible)
            return;

        X = x;
        Z = z;
        IsVisible = visible;
        Changed?.Invoke();
    }

    public void UpdateTool(TerrainBrushMode mode, float radius, float strength, float falloff)
    {
        radius = Math.Clamp(radius, 1f, 32f);
        strength = Math.Clamp(strength, .01f, 1f);
        falloff = Math.Clamp(falloff, 0f, .95f);

        if (Mode == mode &&
            NearlyEqual(Radius, radius) &&
            NearlyEqual(Strength, strength) &&
            NearlyEqual(Falloff, falloff))
            return;

        Mode = mode;
        Radius = radius;
        Strength = strength;
        Falloff = falloff;
        Changed?.Invoke();
    }

    public void Hide()
    {
        if (!IsVisible)
            return;

        IsVisible = false;
        Changed?.Invoke();
    }

    private static bool NearlyEqual(float left, float right) => MathF.Abs(left - right) < .001f;
}
