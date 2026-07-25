using System.Numerics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace Vexis.Rendering;

/// <summary>
/// Owns the native Direct3D 11 device, immediate context, HWND swap chain,
/// back-buffer render target, depth buffer, viewport, resize, clear, present,
/// and deterministic COM disposal.
///
/// Step 2 intentionally clears and presents only. Terrain and water draw calls
/// are connected in later steps.
/// </summary>
public sealed class Direct3D11GraphicsDevice : IGraphicsDevice
{
    private ID3D11Device? _device;
    private ID3D11DeviceContext? _context;
    private IDXGIFactory2? _factory;
    private IDXGISwapChain1? _swapChain;
    private ID3D11RenderTargetView? _renderTargetView;
    private ID3D11Texture2D? _depthTexture;
    private ID3D11DepthStencilView? _depthStencilView;
    private bool _frameOpen;
    private bool _disposed;

    public bool IsInitialized { get; private set; }
    public GraphicsBackend Backend => GraphicsBackend.Direct3D11;
    public ViewportSize Viewport { get; private set; } = new(1, 1);
    public FeatureLevel FeatureLevel { get; private set; }
    public string AdapterDescription { get; private set; } = "Uninitialized";

    public void Initialize(nint windowHandle, int width, int height)
    {
        ThrowIfDisposed();

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Direct3D 11 is only available on Windows.");

        if (windowHandle == 0)
            throw new ArgumentException("A valid Win32 HWND is required.", nameof(windowHandle));

        if (IsInitialized)
            throw new InvalidOperationException("The Direct3D 11 graphics device is already initialized.");

        Viewport = new ViewportSize(width, height).ClampToValid();

        var flags = DeviceCreationFlags.BgraSupport;
#if DEBUG
        flags |= DeviceCreationFlags.Debug;
#endif

        FeatureLevel[] requestedFeatureLevels =
        [
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0
        ];

        var result = D3D11CreateDevice(
            null,
            DriverType.Hardware,
            flags,
            requestedFeatureLevels,
            out _device,
            out var selectedFeatureLevel,
            out _context);

        // A machine may have Direct3D 11 but not the optional SDK debug layer.
        if (result.Failure && flags.HasFlag(DeviceCreationFlags.Debug))
        {
            flags &= ~DeviceCreationFlags.Debug;
            result = D3D11CreateDevice(
                null,
                DriverType.Hardware,
                flags,
                requestedFeatureLevels,
                out _device,
                out selectedFeatureLevel,
                out _context);
        }

        result.CheckError();

        FeatureLevel = selectedFeatureLevel;
        _factory = CreateDXGIFactory2<IDXGIFactory2>(false);

        using (var dxgiDevice = _device.QueryInterface<IDXGIDevice>())
        using (var adapter = dxgiDevice.GetAdapter())
        {
            AdapterDescription = adapter.Description.Description.TrimEnd('\0');
        }

        var swapChainDescription = new SwapChainDescription1
        {
            Width = (uint)Viewport.Width,
            Height = (uint)Viewport.Height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = SampleDescription.Default,
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.None
        };

        var fullscreenDescription = new SwapChainFullscreenDescription
        {
            Windowed = true
        };

        _swapChain = _factory.CreateSwapChainForHwnd(
            _device,
            windowHandle,
            swapChainDescription,
            fullscreenDescription);

        _factory.MakeWindowAssociation(windowHandle, WindowAssociationFlags.IgnoreAltEnter);

        CreateSizeDependentResources();
        IsInitialized = true;
    }

    public void Resize(int width, int height)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        var next = new ViewportSize(width, height).ClampToValid();
        if (next == Viewport)
            return;

        _context!.OMSetRenderTargets(Array.Empty<ID3D11RenderTargetView>(), null);
        ReleaseSizeDependentResources();

        _swapChain!.ResizeBuffers(
            0,
            (uint)next.Width,
            (uint)next.Height,
            Format.Unknown,
            SwapChainFlags.None).CheckError();

        Viewport = next;
        CreateSizeDependentResources();
    }

    public void BeginFrame(RenderFrameContext frame)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        if (_frameOpen)
            throw new InvalidOperationException("BeginFrame cannot be called twice without EndFrame.");

        var clear = frame.ClearColor;
        _context!.OMSetRenderTargets(_renderTargetView!, _depthStencilView);
        _context.RSSetViewport(new Viewport(
            0f,
            0f,
            Viewport.Width,
            Viewport.Height,
            0f,
            1f));

        _context.ClearRenderTargetView(
            _renderTargetView!,
            new Color4(clear.X, clear.Y, clear.Z, clear.W));

        _context.ClearDepthStencilView(
            _depthStencilView!,
            DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
            1f,
            0);

        _frameOpen = true;
    }

    public void Render(RenderScene scene)
    {
        ThrowIfDisposed();
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(scene);

        if (!_frameOpen)
            throw new InvalidOperationException("BeginFrame must be called before Render.");

        // Step 2 establishes the real GPU frame lifecycle.
        // Terrain, object, and water draw submissions are added in later steps.
    }

    public void EndFrame()
    {
        ThrowIfDisposed();
        EnsureInitialized();

        if (!_frameOpen)
            throw new InvalidOperationException("BeginFrame must be called before EndFrame.");

        try
        {
            _swapChain!.Present(1, PresentFlags.None).CheckError();
        }
        finally
        {
            _frameOpen = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _frameOpen = false;
        IsInitialized = false;

        if (_context is not null)
        {
            _context.ClearState();
            _context.Flush();
        }

        ReleaseSizeDependentResources();
        _swapChain?.Dispose();
        _factory?.Dispose();
        _context?.Dispose();
        _device?.Dispose();

        _swapChain = null;
        _factory = null;
        _context = null;
        _device = null;
        GC.SuppressFinalize(this);
    }

    private void CreateSizeDependentResources()
    {
        using var backBuffer = _swapChain!.GetBuffer<ID3D11Texture2D>(0);
        _renderTargetView = _device!.CreateRenderTargetView(backBuffer);

        var depthDescription = new Texture2DDescription
        {
            Width = (uint)Viewport.Width,
            Height = (uint)Viewport.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.D24_UNorm_S8_UInt,
            SampleDescription = SampleDescription.Default,
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        _depthTexture = _device.CreateTexture2D(depthDescription);
        _depthStencilView = _device.CreateDepthStencilView(_depthTexture);
    }

    private void ReleaseSizeDependentResources()
    {
        _depthStencilView?.Dispose();
        _depthTexture?.Dispose();
        _renderTargetView?.Dispose();

        _depthStencilView = null;
        _depthTexture = null;
        _renderTargetView = null;
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized ||
            _device is null ||
            _context is null ||
            _swapChain is null ||
            _renderTargetView is null ||
            _depthStencilView is null)
        {
            throw new InvalidOperationException("The Direct3D 11 graphics device has not been initialized.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
