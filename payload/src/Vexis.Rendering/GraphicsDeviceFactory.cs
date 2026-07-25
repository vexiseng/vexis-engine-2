namespace Vexis.Rendering;

public static class GraphicsDeviceFactory
{
    public static IGraphicsDevice Create(GraphicsBackend backend) => backend switch
    {
        GraphicsBackend.Direct3D11 when Direct3D11Availability.IsSupported =>
            new Direct3D11GraphicsDevice(),
        GraphicsBackend.Software =>
            new SoftwareGraphicsDevice(),
        GraphicsBackend.Vulkan =>
            throw new NotSupportedException("The Vulkan backend has not been implemented yet."),
        GraphicsBackend.Direct3D11 =>
            throw new PlatformNotSupportedException("Direct3D 11 requires Windows 7 or newer."),
        _ =>
            throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unknown graphics backend.")
    };

    public static IGraphicsDevice CreateBestAvailable(GraphicsBackend? requested = null)
    {
        var backend = GraphicsBackendSelector.Select(
            requested,
            OperatingSystem.IsWindows(),
            Direct3D11Availability.IsSupported);

        return Create(backend);
    }
}
