namespace Vexis.Rendering;

public static class GraphicsBackendSelector
{
    public static GraphicsBackend Select(
        GraphicsBackend? requested,
        bool isWindows,
        bool direct3D11Available,
        bool vulkanAvailable = false)
    {
        if (requested is { } explicitBackend)
            return IsAvailable(explicitBackend, isWindows, direct3D11Available, vulkanAvailable)
                ? explicitBackend
                : GraphicsBackend.Software;

        if (isWindows && direct3D11Available)
            return GraphicsBackend.Direct3D11;

        if (vulkanAvailable)
            return GraphicsBackend.Vulkan;

        return GraphicsBackend.Software;
    }

    private static bool IsAvailable(
        GraphicsBackend backend,
        bool isWindows,
        bool direct3D11Available,
        bool vulkanAvailable) => backend switch
    {
        GraphicsBackend.Software => true,
        GraphicsBackend.Direct3D11 => isWindows && direct3D11Available,
        GraphicsBackend.Vulkan => vulkanAvailable,
        _ => false
    };
}
