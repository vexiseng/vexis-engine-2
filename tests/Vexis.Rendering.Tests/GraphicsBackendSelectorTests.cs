using Xunit;
using Vexis.Rendering;

namespace Vexis.Rendering.Tests;

public sealed class GraphicsBackendSelectorTests
{
    [Fact]
    public void Automatic_prefers_direct3d11_on_windows()
    {
        var selected = GraphicsBackendSelector.Select(null, true, true);
        Assert.Equal(GraphicsBackend.Direct3D11, selected);
    }

    [Fact]
    public void Unavailable_explicit_backend_falls_back_to_software()
    {
        var selected = GraphicsBackendSelector.Select(GraphicsBackend.Direct3D11, false, false);
        Assert.Equal(GraphicsBackend.Software, selected);
    }
}
