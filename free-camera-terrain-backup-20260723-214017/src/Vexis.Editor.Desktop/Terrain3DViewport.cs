using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Numerics;

namespace Vexis.Editor.Desktop;

/// <summary>
/// Real-time perspective preview of the canonical editor terrain. This is a CPU-rendered
/// editor preview so it has no external graphics dependency and always reflects terrain edits.
/// The runtime GPU renderer can replace the drawing backend without changing the world model.
/// </summary>
public sealed class Terrain3DViewport : Control
{
    private readonly EditorState _state;
    private Point _lastPointer;
    private bool _orbiting;
    private bool _panning;
    private float _yaw = -0.72f;
    private float _pitch = 0.72f;
    private float _distance = 92f;
    private Vector2 _targetOffset;

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
        PointerReleased += (_, _) => { _orbiting = false; _panning = false; };
        PointerWheelChanged += OnPointerWheel;
        _state.Changed += InvalidateVisual;
        _state.BrushPreview.Changed += InvalidateVisual;
    }

    public void ResetCamera()
    {
        _yaw = -0.72f;
        _pitch = 0.72f;
        _distance = 92f;
        _targetOffset = Vector2.Zero;
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
        var range = Math.Max(0.001f, max - min);
        var projection = new Projection(Bounds, terrain, _yaw, _pitch, _distance, _targetOffset, HeightScale);
        var cells = new List<RenderedCell>((terrain.Width - 1) * (terrain.Height - 1));

        for (var z = 0; z < terrain.Height - 1; z++)
        for (var x = 0; x < terrain.Width - 1; x++)
        {
            var a = projection.Project(x, terrain[x, z], z);
            var b = projection.Project(x + 1, terrain[x + 1, z], z);
            var c = projection.Project(x + 1, terrain[x + 1, z + 1], z + 1);
            var d = projection.Project(x, terrain[x, z + 1], z + 1);
            if (!a.Visible || !b.Visible || !c.Visible || !d.Visible) continue;

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
        var outer = BuildTerrainRing(projection, brush.X, brush.Z, brush.Radius);
        if (outer.Count < 3)
            return;

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(outer[0], true);
            foreach (var point in outer.Skip(1))
                stream.LineTo(point);
            stream.EndFigure(true);
        }

        context.DrawGeometry(
            new SolidColorBrush(Color.FromArgb(36, color.R, color.G, color.B)),
            new Pen(new SolidColorBrush(Color.FromArgb(240, color.R, color.G, color.B)), 2),
            geometry);

        var innerRadius = brush.Radius * (1 - brush.Falloff);
        if (innerRadius <= .25f)
            return;

        var inner = BuildTerrainRing(projection, brush.X, brush.Z, innerRadius);
        if (inner.Count < 3)
            return;

        var innerGeometry = new StreamGeometry();
        using (var stream = innerGeometry.Open())
        {
            stream.BeginFigure(inner[0], false);
            foreach (var point in inner.Skip(1))
                stream.LineTo(point);
            stream.EndFigure(true);
        }

        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(175, color.R, color.G, color.B)), 1.2),
            innerGeometry);
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
            if (!bottom.Visible || !top.Visible) continue;

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
        var title = new FormattedText("LIVE 3D WORLD PREVIEW", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 14, Brushes.White);
        context.DrawText(title, new Point(14, 12));
        var detail = new FormattedText($"Terrain {terrain.Width}×{terrain.Height}  •  Revision {terrain.Revision}  •  Objects {_state.Objects.Count}", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray);
        context.DrawText(detail, new Point(14, 34));
        var controls = new FormattedText("Left-drag orbit  •  Middle-drag pan  •  Wheel zoom  •  R reset camera", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray);
        context.DrawText(controls, new Point(14, Math.Max(14, Bounds.Height - 28)));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        _lastPointer = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;
        _orbiting = properties.IsLeftButtonPressed;
        _panning = properties.IsMiddleButtonPressed || properties.IsRightButtonPressed;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var current = e.GetPosition(this);
        var delta = current - _lastPointer;
        _lastPointer = current;

        if (_orbiting)
        {
            _yaw += (float)delta.X * .008f;
            _pitch = Math.Clamp(_pitch + (float)delta.Y * .006f, .12f, 1.42f);
            InvalidateVisual();
        }
        else if (_panning)
        {
            var scale = _distance / 700f;
            _targetOffset += new Vector2((float)-delta.X * scale, (float)delta.Y * scale);
            InvalidateVisual();
        }
    }

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        _distance = Math.Clamp(_distance * (e.Delta.Y > 0 ? .90f : 1.11f), 18f, 260f);
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.R)
        {
            ResetCamera();
            e.Handled = true;
        }
    }

    private static Color BrushColor(TerrainBrushMode mode) => mode switch
    {
        TerrainBrushMode.Raise => Color.Parse("#6FE38C"),
        TerrainBrushMode.Lower => Color.Parse("#64B5F6"),
        TerrainBrushMode.Smooth => Color.Parse("#FFD166"),
        TerrainBrushMode.Flatten => Color.Parse("#E78BFA"),
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
        private readonly float _centerX;
        private readonly float _centerZ;
        private readonly float _cosYaw;
        private readonly float _sinYaw;
        private readonly float _cosPitch;
        private readonly float _sinPitch;
        private readonly float _distance;
        private readonly Vector2 _offset;
        private readonly float _heightScale;
        private readonly float _focalLength;

        public Projection(Rect bounds, TerrainDocument terrain, float yaw, float pitch, float distance, Vector2 offset, float heightScale)
        {
            _bounds = bounds;
            _centerX = (terrain.Width - 1) * .5f;
            _centerZ = (terrain.Height - 1) * .5f;
            _cosYaw = MathF.Cos(yaw);
            _sinYaw = MathF.Sin(yaw);
            _cosPitch = MathF.Cos(pitch);
            _sinPitch = MathF.Sin(pitch);
            _distance = distance;
            _offset = offset;
            _heightScale = heightScale;
            _focalLength = (float)Math.Min(bounds.Width, bounds.Height) * 1.18f;
        }

        public ProjectedPoint Project(float x, float height, float z)
        {
            var dx = x - _centerX - _offset.X;
            var dz = z - _centerZ - _offset.Y;
            var dy = height * _heightScale;

            var rx = _cosYaw * dx - _sinYaw * dz;
            var rz = _sinYaw * dx + _cosYaw * dz;
            var ry = _cosPitch * dy - _sinPitch * rz;
            var depth = _sinPitch * dy + _cosPitch * rz + _distance;
            if (depth <= 1f) return new ProjectedPoint(default, depth, false);

            var scale = _focalLength / depth;
            var screen = new Point(_bounds.Width * .5 + rx * scale, _bounds.Height * .54 - ry * scale);
            return new ProjectedPoint(screen, depth, true);
        }

        public float DepthAt(float x, float height, float z) => Project(x, height, z).Depth;
    }
}
