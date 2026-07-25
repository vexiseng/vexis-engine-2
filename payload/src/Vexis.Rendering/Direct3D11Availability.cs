namespace Vexis.Rendering;

public static class Direct3D11Availability
{
    public static bool IsSupported =>
        OperatingSystem.IsWindowsVersionAtLeast(6, 1);
}
