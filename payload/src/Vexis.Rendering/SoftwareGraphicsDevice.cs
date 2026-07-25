namespace Vexis.Rendering;

public sealed class SoftwareGraphicsDevice : IGraphicsDevice
{
    private bool _disposed;

    public bool IsInitialized { get; private set; }
    public GraphicsBackend Backend => GraphicsBackend.Software;
    public ViewportSize Viewport { get; private set; }

    public void Initialize(nint windowHandle, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Viewport = new ViewportSize(width, height);
        IsInitialized = !Viewport.IsEmpty;
    }

    public void Resize(int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Viewport = new ViewportSize(width, height);
        IsInitialized = !Viewport.IsEmpty;
    }

    public void BeginFrame(RenderFrameContext context)
    {
        EnsureReady();
    }

    public void Render(RenderScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        EnsureReady();
    }

    public void EndFrame()
    {
        EnsureReady();
    }

    public void Dispose()
    {
        _disposed = true;
        IsInitialized = false;
    }

    private void EnsureReady()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsInitialized)
            throw new InvalidOperationException("The graphics device has not been initialized with a non-empty viewport.");
    }
}
