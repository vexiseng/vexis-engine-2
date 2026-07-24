using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Vexis.World;

namespace Vexis.Editor.Desktop;

public sealed class WorldMapView : Control
{
    private double _zoom = 1;
    private Vector _pan;
    private Point _last;
    private bool _drag;
    private EditorState? _state;

    public WorldMapView() { ClipToBounds = true; PointerWheelChanged += (_, e) => { _zoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.2 : .833), .4, 8); InvalidateVisual(); }; PointerPressed += (_, e) => { _drag = true; _last = e.GetPosition(this); }; PointerReleased += (_, _) => _drag = false; PointerMoved += (_, e) => { if (!_drag) return; var p = e.GetPosition(this); _pan += p - _last; _last = p; InvalidateVisual(); }; }

    public void Bind(EditorState state) => _state = state;

    public override void Render(DrawingContext dc)
    {
        dc.FillRectangle(new SolidColorBrush(Color.Parse("#121820")), Bounds);
        var center = new Point(Bounds.Width / 2 + _pan.X, Bounds.Height / 2 + _pan.Y);
        var state = _state;
        var terrain = state?.Terrain;
        if (terrain is null)
        {
            DrawPlaceholder(dc, center);
            return;
        }

        double cell = Math.Max(8, Math.Min(Bounds.Width / terrain.Width, Bounds.Height / terrain.Height) * .85 * _zoom);
        for (var y = 0; y < terrain.Height; y++)
        for (var x = 0; x < terrain.Width; x++)
        {
            var rect = new Rect(center.X + x * cell - terrain.Width * cell / 2, center.Y + y * cell - terrain.Height * cell / 2, cell, cell);
            var height = terrain[x, y];
            var color = HeightColor(height);
            dc.FillRectangle(new SolidColorBrush(color), rect);
            dc.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#1C242B")), .35), rect);
        }

        if (state?.WaterBodies.Count > 0)
        {
            foreach (var body in state.WaterBodies)
            {
                if (body.Seeds.Count == 0) continue;
                var seed = body.Seeds[0];
                var seedPoint = new Point(center.X + seed.X * cell - terrain.Width * cell / 2, center.Y + seed.Z * cell - terrain.Height * cell / 2);
                var rect = new Rect(seedPoint.X - 6 * _zoom, seedPoint.Y - 6 * _zoom, 12 * _zoom, 12 * _zoom);
                dc.DrawEllipse(new SolidColorBrush(Color.Parse("#4B90C2")), new Pen(Brushes.Transparent, 0), seedPoint, 6 * _zoom, 6 * _zoom);
            }
        }

        if (state?.Objects.Count > 0)
        {
            foreach (var obj in state.Objects)
            {
                var point = new Point(center.X + obj.Position.X * cell - terrain.Width * cell / 2, center.Y + obj.Position.Z * cell - terrain.Height * cell / 2);
                dc.DrawEllipse(Brushes.White, new Pen(Brushes.Black, 1), point, 3 * _zoom, 3 * _zoom);
            }
        }

        dc.DrawText(new FormattedText($"VAELOR WORLD MAP • {state?.WaterBodies.Count ?? 0} water body(s) • {state?.Objects.Count ?? 0} objects", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 16, Brushes.White), new Point(15, 15));
        dc.DrawText(new FormattedText("Drag to pan • Wheel to zoom • terrain / water / objects", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.LightGray), new Point(15, 42));
    }

    private static void DrawPlaceholder(DrawingContext dc, Point center)
    {
        var rect = new Rect(center.X - 140, center.Y - 140, 280, 280);
        dc.FillRectangle(new SolidColorBrush(Color.Parse("#2A3A45")), rect);
        dc.DrawText(new FormattedText("World map preview pending", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 16, Brushes.White), new Point(15, 15));
    }

    private static Color HeightColor(float height)
    {
        if (height < -1.5f) return Color.Parse("#254C67");
        if (height < .3f) return Color.Parse("#486B45");
        if (height < 2f) return Color.Parse("#60774C");
        if (height < 6f) return Color.Parse("#7A6B4B");
        return Color.Parse("#8B8B82");
    }
}
