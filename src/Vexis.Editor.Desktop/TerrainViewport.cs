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
    private TerrainBrushMode _brushMode = TerrainBrushMode.Raise;
    private float _brushRadius = 4;
    private float _brushStrength = .22f;
    private float _brushFalloff = .55f;

    public TerrainBrushMode BrushMode
    {
        get => _brushMode;
        set
        {
            _brushMode = value;
            SyncBrushPreview();
        }
    }

    public float BrushRadius
    {
        get => _brushRadius;
        set
        {
            _brushRadius = Math.Clamp(value, 1, 32);
            SyncBrushPreview();
        }
    }

    public float BrushStrength
    {
        get => _brushStrength;
        set
        {
            _brushStrength = Math.Clamp(value, .01f, 1f);
            SyncBrushPreview();
        }
    }

    public float BrushFalloff
    {
        get => _brushFalloff;
        set
        {
            _brushFalloff = Math.Clamp(value, 0, .95f);
            SyncBrushPreview();
        }
    }

    public TerrainViewport(EditorState state)
    {
        _state = state;
        Focusable = true;
        ClipToBounds = true;
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += (_, _) => { if (_painting) _state.EndTerrainStroke(); _painting = false; _panning = false; };
        PointerWheelChanged += OnWheel;
        PointerExited += (_, _) => _state.BrushPreview.Hide();
        SyncBrushPreview();
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

        DrawAdaptiveGrid(context, terrain, cell, origin);
        DrawObjects(context, terrain, cell, origin);
        DrawBrush(context, terrain, cell, origin);
        DrawOverlay(context, terrain, cell);
    }

    private void DrawAdaptiveGrid(DrawingContext context, TerrainDocument terrain, double cell, Point origin)
    {
        var terrainRect = new Rect(origin.X, origin.Y, terrain.Width * cell, terrain.Height * cell);

        if (cell >= 12)
            DrawGridLayer(context, terrain, cell, origin, 1, Color.FromArgb(45, 255, 255, 255), .45);

        if (cell >= 3.5)
            DrawGridLayer(context, terrain, cell, origin, 8, Color.FromArgb(125, 225, 235, 241), .85);

        DrawGridLayer(context, terrain, cell, origin, 64, Color.Parse("#F0E9B872"), 2);

        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#FFE9B872")), 2), terrainRect);

        if (cell * 64 >= 110)
            DrawRegionLabels(context, terrain, cell, origin);
    }

    private static void DrawGridLayer(DrawingContext context, TerrainDocument terrain, double cell, Point origin, int spacing, Color color, double thickness)
    {
        var pen = new Pen(new SolidColorBrush(color), thickness);

        for (var x = 0; x <= terrain.Width; x += spacing)
        {
            var screenX = origin.X + x * cell;
            context.DrawLine(pen, new Point(screenX, origin.Y), new Point(screenX, origin.Y + terrain.Height * cell));
        }

        for (var y = 0; y <= terrain.Height; y += spacing)
        {
            var screenY = origin.Y + y * cell;
            context.DrawLine(pen, new Point(origin.X, screenY), new Point(origin.X + terrain.Width * cell, screenY));
        }
    }

    private static void DrawRegionLabels(DrawingContext context, TerrainDocument terrain, double cell, Point origin)
    {
        for (var regionY = 0; regionY < terrain.Height; regionY += 64)
        for (var regionX = 0; regionX < terrain.Width; regionX += 64)
        {
            var label = $"R {regionX / 64},{regionY / 64}";
            var text = new FormattedText(
                label,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                12,
                new SolidColorBrush(Color.Parse("#FFE9B872")));

            context.DrawText(text, new Point(origin.X + regionX * cell + 7, origin.Y + regionY * cell + 6));
        }
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
        var outerRadius = BrushRadius * cell;
        var innerRadius = outerRadius * (1 - BrushFalloff);
        var color = BrushColor(_lowering ? TerrainBrushMode.Lower : BrushMode);

        context.DrawEllipse(
            new SolidColorBrush(Color.FromArgb(42, color.R, color.G, color.B)),
            new Pen(new SolidColorBrush(Color.FromArgb(245, color.R, color.G, color.B)), 2),
            center,
            outerRadius,
            outerRadius);

        if (innerRadius > 2)
            context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(190, color.R, color.G, color.B)), 1.25), center, innerRadius, innerRadius);

        context.DrawLine(new Pen(Brushes.White, 1.25), new Point(center.X - 6, center.Y), new Point(center.X + 6, center.Y));
        context.DrawLine(new Pen(Brushes.White, 1.25), new Point(center.X, center.Y - 6), new Point(center.X, center.Y + 6));
    }

    private void DrawOverlay(DrawingContext context, TerrainDocument terrain, double cell)
    {
        var gridMode = cell >= 12 ? "Tile + Chunk + Region" : cell >= 3.5 ? "Chunk + Region" : "Region";
        var text = $"WORLD TERRAIN  •  {BrushMode}  •  Radius {BrushRadius:0.0}  •  Strength {BrushStrength:0.00}  •  Falloff {BrushFalloff:P0}";
        context.DrawText(new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 13, Brushes.White), new Point(14, 12));
        context.DrawText(new FormattedText("Left-drag sculpt • Right-drag lower • MMB pan • Wheel zoom • Shift+wheel brush size", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray), new Point(14, Bounds.Height - 28));
        context.DrawText(new FormattedText($"{terrain.Width}×{terrain.Height} cells • {gridMode} grid • Revision {terrain.Revision}", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray), new Point(Math.Max(14, Bounds.Width - 330), 12));
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        _pointer = _last = e.GetPosition(this);
        UpdateBrushPosition();
        var props = e.GetCurrentPoint(this).Properties;
        _panning = props.IsMiddleButtonPressed;
        _painting = props.IsLeftButtonPressed || props.IsRightButtonPressed;
        _lowering = props.IsRightButtonPressed;
        if (_painting) _state.BeginTerrainStroke();
        SyncBrushPreview();
        if (_painting) ApplyBrush(_pointer);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        _pointer = e.GetPosition(this);
        var delta = _pointer - _last;
        _last = _pointer;
        if (_panning) _pan += new Avalonia.Vector(delta.X, delta.Y);
        UpdateBrushPosition();
        if (_painting) ApplyBrush(_pointer);
        InvalidateVisual();
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            BrushRadius = BrushRadius + (float)e.Delta.Y;
        else
            _zoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.1 : .9), .35, 8);
        UpdateBrushPosition();
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

    private void UpdateBrushPosition()
    {
        if (!Bounds.Contains(_pointer))
        {
            _state.BrushPreview.Hide();
            return;
        }

        var terrain = _state.Terrain;
        var cell = CellSize(terrain);
        var world = ScreenToTerrain(_pointer, terrain, cell, Origin(terrain, cell));
        _state.BrushPreview.UpdatePosition((float)world.X, (float)world.Y, true);
        SyncBrushPreview();
    }

    private void SyncBrushPreview()
    {
        _state.BrushPreview.UpdateTool(
            _lowering ? TerrainBrushMode.Lower : BrushMode,
            BrushRadius,
            BrushStrength,
            BrushFalloff);
    }

    private double CellSize(TerrainDocument terrain) => Math.Max(2, Math.Min(Bounds.Width / terrain.Width, Bounds.Height / terrain.Height) * .9 * _zoom);
    private Point Origin(TerrainDocument terrain, double cell) => new(Bounds.Width / 2 - terrain.Width * cell / 2 + _pan.X, Bounds.Height / 2 - terrain.Height * cell / 2 + _pan.Y);
    private static Point ScreenToTerrain(Point p, TerrainDocument terrain, double cell, Point origin) => new(Math.Clamp((p.X - origin.X) / cell, 0, terrain.Width - 1), Math.Clamp((p.Y - origin.Y) / cell, 0, terrain.Height - 1));

    private static Color BrushColor(TerrainBrushMode mode) => mode switch
    {
        TerrainBrushMode.Raise => Color.Parse("#6FE38C"),
        TerrainBrushMode.Lower => Color.Parse("#64B5F6"),
        TerrainBrushMode.Smooth => Color.Parse("#FFD166"),
        TerrainBrushMode.Flatten => Color.Parse("#E78BFA"),
        _ => Colors.White
    };

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
