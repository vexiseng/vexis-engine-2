using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System.Numerics;

namespace Vexis.Editor.Desktop;

/// <summary>
/// CPU-rendered perspective preview with a true six-degree editor free camera.
/// Hold the right mouse button to look. Use WASD to fly horizontally and Q/E vertically.
/// </summary>
public sealed class Terrain3DViewport : Control
{
    private readonly EditorState _state;
    private readonly DispatcherTimer _movementTimer;
    private readonly HashSet<Key> _pressedKeys = [];

    private Point _lastPointer;
    private bool _mouseLooking;
    private Vector3 _cameraPosition = new(32f, 30f, 72f);
    private float _yaw = -2.72f;
    private float _pitch = -0.34f;
    private float _moveSpeed = 22f;
    private DateTime _lastTick = DateTime.UtcNow;

    public bool ShowWireframe { get; set; } = true;
    public bool ShowObjects { get; set; } = true;
    public float HeightScale { get; set; } = 3.2f;

    public Terrain3DViewport(EditorState state)
    {
        _state = state;
        Focusable = true;
        ClipToBounds = true;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheel;
        KeyDown += OnViewportKeyDown;
        KeyUp += OnViewportKeyUp;
        LostFocus += (_, _) => _pressedKeys.Clear();

        _state.Changed += InvalidateVisual;
        _state.BrushPreview.Changed += InvalidateVisual;

        _movementTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _movementTimer.Tick += (_, _) => UpdateMovement();
        _movementTimer.Start();
    }

    public void ResetCamera()
    {
        _cameraPosition = new Vector3(32f, 30f, 72f);
        _yaw = -2.72f;
        _pitch = -0.34f;
        _moveSpeed = 22f;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#182028")), Bounds);

        if (Bounds.Width < 20 || Bounds.Height < 20)
            return;

        var terrain = _state.Terrain;
        var heights = terrain.CopyHeights();
        var min = heights.Min();
        var max = heights.Max();
        var range = Math.Max(.001f, max - min);
        var projection = new Projection(Bounds, _cameraPosition, _yaw, _pitch, HeightScale);
        var cells = new List<RenderedCell>((terrain.Width - 1) * (terrain.Height - 1));

        for (var z = 0; z < terrain.Height - 1; z++)
        for (var x = 0; x < terrain.Width - 1; x++)
        {
            var a = projection.Project(x, terrain[x, z], z);
            var b = projection.Project(x + 1, terrain[x + 1, z], z);
            var c = projection.Project(x + 1, terrain[x + 1, z + 1], z + 1);
            var d = projection.Project(x, terrain[x, z + 1], z + 1);
            if (!a.Visible || !b.Visible || !c.Visible || !d.Visible)
                continue;

            var averageHeight = (terrain[x, z] + terrain[x + 1, z] + terrain[x + 1, z + 1] + terrain[x, z + 1]) * .25f;
            var normalized = (averageHeight - min) / range;
            var slopeX = MathF.Abs(terrain[x + 1, z] - terrain[x, z]);
            var slopeZ = MathF.Abs(terrain[x, z + 1] - terrain[x, z]);
            var shade = Math.Clamp(1f - (slopeX + slopeZ) * .055f, .62f, 1.08f);
            cells.Add(new RenderedCell(a, b, c, d, (a.Depth + b.Depth + c.Depth + d.Depth) * .25f, TerrainColor(normalized, averageHeight, shade)));
        }

        foreach (var cell in cells.OrderByDescending(c => c.Depth))
            DrawCell(context, cell);

        if (_state.BrushPreview.IsVisible)
            DrawBrushPreview(context, projection);

        if (ShowObjects)
            DrawObjects(context, projection);

        DrawOverlay(context, terrain);
    }

    private void UpdateMovement()
    {
        var now = DateTime.UtcNow;
        var deltaSeconds = Math.Clamp((float)(now - _lastTick).TotalSeconds, 0f, .1f);
        _lastTick = now;

        if (_pressedKeys.Count == 0)
            return;

        var forward = Forward;
        var flatForward = Vector3.Normalize(new Vector3(forward.X, 0f, forward.Z));
        var right = Vector3.Normalize(Vector3.Cross(flatForward, Vector3.UnitY));
        var direction = Vector3.Zero;

        if (_pressedKeys.Contains(Key.W)) direction += flatForward;
        if (_pressedKeys.Contains(Key.S)) direction -= flatForward;
        if (_pressedKeys.Contains(Key.D)) direction += right;
        if (_pressedKeys.Contains(Key.A)) direction -= right;
        if (_pressedKeys.Contains(Key.E) || _pressedKeys.Contains(Key.Space)) direction += Vector3.UnitY;
        if (_pressedKeys.Contains(Key.Q) || _pressedKeys.Contains(Key.C)) direction -= Vector3.UnitY;

        if (direction.LengthSquared() <= .0001f)
            return;

        direction = Vector3.Normalize(direction);
        var multiplier = _pressedKeys.Contains(Key.LeftShift) || _pressedKeys.Contains(Key.RightShift) ? 4f
            : _pressedKeys.Contains(Key.LeftCtrl) || _pressedKeys.Contains(Key.RightCtrl) ? .25f
            : 1f;

        _cameraPosition += direction * _moveSpeed * multiplier * deltaSeconds;
        InvalidateVisual();
    }

    private Vector3 Forward
    {
        get
        {
            var cosPitch = MathF.Cos(_pitch);
            return Vector3.Normalize(new Vector3(
                MathF.Sin(_yaw) * cosPitch,
                MathF.Sin(_pitch),
                MathF.Cos(_yaw) * cosPitch));
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        _lastPointer = e.GetPosition(this);
        _mouseLooking = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        if (_mouseLooking)
        {
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var current = e.GetPosition(this);
        var delta = current - _lastPointer;
        _lastPointer = current;

        if (!_mouseLooking)
            return;

        _yaw += (float)delta.X * .0065f;
        _pitch = Math.Clamp(_pitch - (float)delta.Y * .0055f, -1.52f, 1.52f);
        InvalidateVisual();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _mouseLooking = false;
        e.Pointer.Capture(null);
    }

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        _moveSpeed = Math.Clamp(_moveSpeed * (e.Delta.Y > 0 ? 1.15f : .87f), 1f, 250f);
        InvalidateVisual();
        e.Handled = true;
    }

    private void OnViewportKeyDown(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Add(e.Key);
        if (e.Key == Key.R)
            ResetCamera();
        e.Handled = IsMovementKey(e.Key) || e.Key == Key.R;
    }

    private void OnViewportKeyUp(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Remove(e.Key);
        e.Handled = IsMovementKey(e.Key);
    }

    private static bool IsMovementKey(Key key) =>
        key is Key.W or Key.A or Key.S or Key.D or Key.Q or Key.E or Key.C or Key.Space
            or Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl;

    private void DrawCell(DrawingContext context, RenderedCell cell)
    {
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(cell.A.Screen, true);
            stream.LineTo(cell.B.Screen);
            stream.LineTo(cell.C.Screen);
            stream.LineTo(cell.D.Screen);
            stream.EndFigure(true);
        }

        var fill = new SolidColorBrush(cell.Color);
        Pen? pen = ShowWireframe ? new Pen(new SolidColorBrush(Color.FromArgb(52, 255, 255, 255)), .45) : null;
        context.DrawGeometry(fill, pen, geometry);
    }

    private void DrawBrushPreview(DrawingContext context, Projection projection)
    {
        var brush = _state.BrushPreview;
        var color = BrushColor(brush.Mode);
        var ring = BuildTerrainRing(projection, brush.X, brush.Z, brush.Radius);
        if (ring.Count < 3)
            return;

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(ring[0], true);
            foreach (var point in ring.Skip(1))
                stream.LineTo(point);
            stream.EndFigure(true);
        }

        context.DrawGeometry(
            new SolidColorBrush(Color.FromArgb(34, color.R, color.G, color.B)),
            new Pen(new SolidColorBrush(Color.FromArgb(240, color.R, color.G, color.B)), 2),
            geometry);
    }

    private List<Point> BuildTerrainRing(Projection projection, float centerX, float centerZ, float radius)
    {
        var points = new List<Point>(49);
        for (var i = 0; i <= 48; i++)
        {
            var angle = MathF.Tau * i / 48f;
            var x = centerX + MathF.Cos(angle) * radius;
            var z = centerZ + MathF.Sin(angle) * radius;
            var projected = projection.Project(x, SampleTerrain(x, z) + .04f, z);
            if (projected.Visible)
                points.Add(projected.Screen);
        }
        return points;
    }

    private void DrawObjects(DrawingContext context, Projection projection)
    {
        foreach (var obj in _state.Objects.OrderByDescending(o => projection.DepthAt(o.Position.X, o.Position.Y, o.Position.Z)))
        {
            var groundHeight = SampleTerrain(obj.Position.X, obj.Position.Z);
            var bottom = projection.Project(obj.Position.X, groundHeight, obj.Position.Z);
            var top = projection.Project(obj.Position.X, groundHeight + Math.Max(.7f, obj.Scale.Y * 1.6f), obj.Position.Z);
            if (!bottom.Visible || !top.Visible)
                continue;

            var selected = ReferenceEquals(obj, _state.Selected);
            var brush = selected ? Brushes.Orange : Brushes.White;
            var pen = new Pen(brush, selected ? 2.5 : 1.5);
            context.DrawLine(pen, bottom.Screen, top.Screen);
            context.DrawEllipse(brush, new Pen(Brushes.Black, 1), top.Screen, selected ? 5 : 3.5, selected ? 5 : 3.5);
        }
    }

    private float SampleTerrain(float x, float z)
    {
        var terrain = _state.Terrain;
        var ix = Math.Clamp((int)MathF.Round(x), 0, terrain.Width - 1);
        var iz = Math.Clamp((int)MathF.Round(z), 0, terrain.Height - 1);
        return terrain[ix, iz];
    }

    private void DrawOverlay(DrawingContext context, TerrainDocument terrain)
    {
        context.DrawText(
            new FormattedText("FREE CAMERA  •  RIGHT-MOUSE LOOK", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 14, Brushes.White),
            new Point(14, 12));

        context.DrawText(
            new FormattedText($"Position {_cameraPosition.X:0.0}, {_cameraPosition.Y:0.0}, {_cameraPosition.Z:0.0}  •  Speed {_moveSpeed:0.0}", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray),
            new Point(14, 34));

        context.DrawText(
            new FormattedText("RMB look • WASD move • Q/E or C/Space down/up • Shift fast • Ctrl slow • Wheel speed • R reset", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray),
            new Point(14, Math.Max(14, Bounds.Height - 28)));
    }

    private static Color BrushColor(TerrainBrushMode mode) => mode switch
    {
        TerrainBrushMode.Raise => Color.Parse("#6FE38C"),
        TerrainBrushMode.Lower => Color.Parse("#64B5F6"),
        TerrainBrushMode.Smooth => Color.Parse("#FFD166"),
        TerrainBrushMode.Flatten or TerrainBrushMode.SetHeight => Color.Parse("#E78BFA"),
        TerrainBrushMode.Noise => Color.Parse("#FF9F68"),
        TerrainBrushMode.Ramp => Color.Parse("#A8DADC"),
        TerrainBrushMode.Erode => Color.Parse("#D4A373"),
        _ => Colors.White
    };

    private static Color TerrainColor(float normalized, float rawHeight, float shade)
    {
        Color baseColor = rawHeight < -.85f ? Color.Parse("#255675")
            : rawHeight < -.35f ? Color.Parse("#397385")
            : normalized < .42f ? Color.Parse("#4E7448")
            : normalized < .68f ? Color.Parse("#687E4D")
            : normalized < .86f ? Color.Parse("#796E54")
            : Color.Parse("#96958A");

        byte Apply(byte value) => (byte)Math.Clamp(value * shade, 0, 255);
        return Color.FromArgb(255, Apply(baseColor.R), Apply(baseColor.G), Apply(baseColor.B));
    }

    private readonly record struct ProjectedPoint(Point Screen, float Depth, bool Visible);
    private readonly record struct RenderedCell(ProjectedPoint A, ProjectedPoint B, ProjectedPoint C, ProjectedPoint D, float Depth, Color Color);

    private sealed class Projection
    {
        private readonly Rect _bounds;
        private readonly Vector3 _cameraPosition;
        private readonly Vector3 _right;
        private readonly Vector3 _up;
        private readonly Vector3 _forward;
        private readonly float _heightScale;
        private readonly float _focalLength;

        public Projection(Rect bounds, Vector3 cameraPosition, float yaw, float pitch, float heightScale)
        {
            _bounds = bounds;
            _cameraPosition = cameraPosition;
            _heightScale = heightScale;
            _forward = Vector3.Normalize(new Vector3(
                MathF.Sin(yaw) * MathF.Cos(pitch),
                MathF.Sin(pitch),
                MathF.Cos(yaw) * MathF.Cos(pitch)));
            _right = Vector3.Normalize(Vector3.Cross(_forward, Vector3.UnitY));
            _up = Vector3.Normalize(Vector3.Cross(_right, _forward));
            _focalLength = (float)Math.Min(bounds.Width, bounds.Height) * 1.05f;
        }

        public ProjectedPoint Project(float x, float height, float z)
        {
            var world = new Vector3(x, height * _heightScale, z);
            var relative = world - _cameraPosition;
            var cameraX = Vector3.Dot(relative, _right);
            var cameraY = Vector3.Dot(relative, _up);
            var depth = Vector3.Dot(relative, _forward);

            if (depth <= .12f)
                return new ProjectedPoint(default, depth, false);

            var scale = _focalLength / depth;
            var screen = new Point(
                _bounds.Width * .5 + cameraX * scale,
                _bounds.Height * .5 - cameraY * scale);

            return new ProjectedPoint(screen, depth, true);
        }

        public float DepthAt(float x, float height, float z) => Project(x, height, z).Depth;
    }
}
