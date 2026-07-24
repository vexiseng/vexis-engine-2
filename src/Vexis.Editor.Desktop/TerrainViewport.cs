using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Vexis.Editor.Desktop;

public sealed class TerrainViewport : Control
{
    private readonly EditorState _state;
    private bool _painting;
    private bool _lowering;
    private Point _pointer;
    private double _zoom = 1;
    private Avalonia.Vector _pan;
    private Point _last;
    private bool _panning;

    public TerrainBrushMode BrushMode { get; set; } = TerrainBrushMode.Raise;
    public float BrushRadius { get; set; } = 4;
    public float BrushStrength { get; set; } = .22f;

    public TerrainViewport(EditorState state)
    {
        _state = state;
        Focusable = true;
        ClipToBounds = true;
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += (_, _) => { _painting = false; _panning = false; };
        PointerWheelChanged += OnWheel;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#11161B")), Bounds);
        var terrain = _state.Terrain;
        var cell = CellSize(terrain);
        var origin = Origin(terrain, cell);
        var min = terrain.CopyHeights().Min();
        var max = terrain.CopyHeights().Max();
        var range = Math.Max(.001f, max - min);

        for (var y = 0; y < terrain.Height - 1; y++)
        for (var x = 0; x < terrain.Width - 1; x++)
        {
            var h = (terrain[x, y] - min) / range;
            var color = HeightColor(h, terrain[x, y]);
            context.FillRectangle(new SolidColorBrush(color), new Rect(origin.X + x * cell, origin.Y + y * cell, cell + .5, cell + .5));
        }

        DrawRegionGrid(context, terrain, cell, origin);
        DrawObjects(context, terrain, cell, origin);
        DrawBrush(context, terrain, cell, origin);
        DrawOverlay(context, terrain);
    }

    private void DrawRegionGrid(DrawingContext context, TerrainDocument terrain, double cell, Point origin)
    {
        var pen = new Pen(new SolidColorBrush(Color.Parse("#90FFFFFF")), 1);
        var major = new Pen(new SolidColorBrush(Color.Parse("#D0E9B872")), 1.5);
        for (var i = 0; i <= terrain.Width; i += 8)
            context.DrawLine(i % 32 == 0 ? major : pen, new Point(origin.X + i * cell, origin.Y), new Point(origin.X + i * cell, origin.Y + terrain.Height * cell));
        for (var i = 0; i <= terrain.Height; i += 8)
            context.DrawLine(i % 32 == 0 ? major : pen, new Point(origin.X, origin.Y + i * cell), new Point(origin.X + terrain.Width * cell, origin.Y + i * cell));
    }

    private void DrawObjects(DrawingContext context, TerrainDocument terrain, double cell, Point origin)
    {
        foreach (var obj in _state.Objects)
        {
            var p = new Point(origin.X + obj.Position.X * cell, origin.Y + obj.Position.Z * cell);
            var selected = ReferenceEquals(obj, _state.Selected);
            context.DrawEllipse(selected ? Brushes.Orange : Brushes.White, new Pen(Brushes.Black, 1), p, selected ? 6 : 4, selected ? 6 : 4);
        }
    }

    private void DrawBrush(DrawingContext context, TerrainDocument terrain, double cell, Point origin)
    {
        if (!Bounds.Contains(_pointer)) return;
        var world = ScreenToTerrain(_pointer, terrain, cell, origin);
        var center = new Point(origin.X + world.X * cell, origin.Y + world.Y * cell);
        context.DrawEllipse(null, new Pen(Brushes.White, 1.5), center, BrushRadius * cell, BrushRadius * cell);
    }

    private void DrawOverlay(DrawingContext context, TerrainDocument terrain)
    {
        var text = $"WORLD TERRAIN  •  {BrushMode}  •  Radius {BrushRadius:0.0}  •  Strength {BrushStrength:0.00}";
        context.DrawText(new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 13, Brushes.White), new Point(14, 12));
        context.DrawText(new FormattedText("Left-drag sculpt • Right-drag lower • MMB pan • Wheel zoom • Shift+wheel brush size", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray), new Point(14, Bounds.Height - 28));
        context.DrawText(new FormattedText($"{terrain.Width}×{terrain.Height} cells • Revision {terrain.Revision}", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray), new Point(Math.Max(14, Bounds.Width - 210), 12));
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        _pointer = _last = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;
        _panning = props.IsMiddleButtonPressed;
        _painting = props.IsLeftButtonPressed || props.IsRightButtonPressed;
        _lowering = props.IsRightButtonPressed;
        if (_painting) ApplyBrush(_pointer);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        _pointer = e.GetPosition(this);
        var delta = _pointer - _last;
        _last = _pointer;
        if (_panning) _pan += new Avalonia.Vector(delta.X, delta.Y);
        if (_painting) ApplyBrush(_pointer);
        InvalidateVisual();
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            BrushRadius = Math.Clamp(BrushRadius + (float)e.Delta.Y, 1, 20);
        else
            _zoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.1 : .9), .35, 5);
        InvalidateVisual();
    }

    private void ApplyBrush(Point point)
    {
        var terrain = _state.Terrain;
        var cell = CellSize(terrain);
        var origin = Origin(terrain, cell);
        var world = ScreenToTerrain(point, terrain, cell, origin);
        var mode = _lowering ? TerrainBrushMode.Lower : BrushMode;
        terrain.Sculpt((int)Math.Round(world.X), (int)Math.Round(world.Y), BrushRadius, BrushStrength, mode);
        _state.MarkDirty();
        InvalidateVisual();
    }

    private double CellSize(TerrainDocument terrain) => Math.Max(2, Math.Min(Bounds.Width / terrain.Width, Bounds.Height / terrain.Height) * .9 * _zoom);
    private Point Origin(TerrainDocument terrain, double cell) => new(Bounds.Width / 2 - terrain.Width * cell / 2 + _pan.X, Bounds.Height / 2 - terrain.Height * cell / 2 + _pan.Y);
    private static Point ScreenToTerrain(Point p, TerrainDocument terrain, double cell, Point origin) => new(Math.Clamp((p.X - origin.X) / cell, 0, terrain.Width - 1), Math.Clamp((p.Y - origin.Y) / cell, 0, terrain.Height - 1));

    private static Color HeightColor(float normalized, float raw)
    {
        if (raw < -.85f) return Color.Parse("#254C67");
        if (raw < -.35f) return Color.Parse("#3E7381");
        if (normalized < .42f) return Color.Parse("#486B45");
        if (normalized < .68f) return Color.Parse("#60774C");
        if (normalized < .86f) return Color.Parse("#746B55");
        return Color.Parse("#8B8B82");
    }
}
