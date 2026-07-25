using Xunit;

namespace Vexis.Rendering.Tests;

public sealed class GraphicsDeviceFactoryTests
{
    [Fact]
    public void ExplicitSoftwareBackendCreatesSoftwareDevice()
    {
        using var device = GraphicsDeviceFactory.Create(GraphicsBackend.Software);

        Assert.Equal(GraphicsBackend.Software, device.Backend);
    }

    [Fact]
    public void VulkanReportsThatItIsNotImplemented()
    {
        Assert.Throws<NotSupportedException>(
            () => GraphicsDeviceFactory.Create(GraphicsBackend.Vulkan));
    }

    [Fact]
    public void Direct3D11AvailabilityMatchesWindowsPlatform()
    {
        Assert.Equal(OperatingSystem.IsWindowsVersionAtLeast(6, 1),
            Direct3D11Availability.IsSupported);
    }
}
