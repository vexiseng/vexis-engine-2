namespace Vexis.Rendering;

public interface IGraphicsDevice : IDisposable
{
    bool IsInitialized { get; }
    GraphicsBackend Backend { get; }
    ViewportSize Viewport { get; }

    void Initialize(nint windowHandle, int width, int height);
    void Resize(int width, int height);
    void BeginFrame(RenderFrameContext context);
    void Render(RenderScene scene);
    void EndFrame();
}
