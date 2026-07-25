using System.Numerics;

namespace Vexis.Rendering;

public sealed class RenderCamera
{
    private const float Epsilon = 0.000001f;

    public Vector3 Position { get; set; } = new(32f, 30f, 72f);
    public float Yaw { get; set; } = -2.72f;
    public float Pitch { get; set; } = -0.34f;
    public float FieldOfViewRadians { get; set; } = MathF.PI / 3f;
    public float NearPlane { get; set; } = 0.05f;
    public float FarPlane { get; set; } = 5000f;

    public Vector3 Forward
    {
        get
        {
            var safePitch = Math.Clamp(Pitch, -1.553343f, 1.553343f);
            var cosPitch = MathF.Cos(safePitch);
            var direction = new Vector3(
                MathF.Sin(Yaw) * cosPitch,
                MathF.Sin(safePitch),
                MathF.Cos(Yaw) * cosPitch);
            return NormalizeOrFallback(direction, Vector3.UnitZ);
        }
    }

    public Vector3 Right => NormalizeOrFallback(Vector3.Cross(Forward, Vector3.UnitY), Vector3.UnitX);
    public Vector3 Up => NormalizeOrFallback(Vector3.Cross(Right, Forward), Vector3.UnitY);

    public Matrix4x4 CreateViewMatrix()
    {
        var forward = Forward;
        return Matrix4x4.CreateLookAt(Position, Position + forward, Up);
    }

    public Matrix4x4 CreateProjectionMatrix(ViewportSize viewport)
    {
        var fov = Math.Clamp(FieldOfViewRadians, 0.174533f, 2.96706f);
        var nearPlane = Math.Max(0.001f, NearPlane);
        var farPlane = Math.Max(nearPlane + 0.001f, FarPlane);
        return Matrix4x4.CreatePerspectiveFieldOfView(fov, viewport.AspectRatio, nearPlane, farPlane);
    }

    public void Look(float deltaYaw, float deltaPitch)
    {
        if (!float.IsFinite(deltaYaw) || !float.IsFinite(deltaPitch))
            return;

        Yaw += deltaYaw;
        Pitch = Math.Clamp(Pitch + deltaPitch, -1.553343f, 1.553343f);
    }

    public void MoveLocal(Vector3 localDelta)
    {
        if (!IsFinite(localDelta))
            return;

        Position += Right * localDelta.X + Vector3.UnitY * localDelta.Y + Forward * localDelta.Z;
    }

    private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
    {
        var lengthSquared = value.LengthSquared();
        return !float.IsFinite(lengthSquared) || lengthSquared < Epsilon
            ? fallback
            : Vector3.Normalize(value);
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
