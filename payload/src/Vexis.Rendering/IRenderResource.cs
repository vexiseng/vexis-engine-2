namespace Vexis.Rendering;

public interface IRenderResource : IDisposable
{
    bool IsDisposed { get; }
    string DebugName { get; }
}
