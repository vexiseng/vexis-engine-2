using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Vexis.Modeling;

namespace Vexis.Editor.Desktop;

public sealed class ModelViewport : Control
{
    private readonly EditorState _state;
    private Point _last;
    private bool _orbiting;
    private bool _panning;
    private double _yaw = -0.65;
    private double _pitch = 0.45;
    private double _zoom = 70;
    private Vector2 _pan;

    public ModelViewport(EditorState state)
    {
        _state = state;
        Focusable = true;
        ClipToBounds = true;
        _state.Changed += InvalidateVisual;
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += (_, _) => { _orbiting = false; _panning = false; };
        PointerWheelChanged += (_, e) => { _zoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.12 : .89), 15, 500); InvalidateVisual(); };
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#171A1F")), Bounds);
        DrawGrid(context);
        foreach (var obj in _state.Objects) DrawMesh(context, obj, ReferenceEquals(obj, _state.Selected));
        DrawAxis(context);
        DrawOverlay(context);
    }

    private void DrawGrid(DrawingContext dc)
    {
        var minor = new Pen(new SolidColorBrush(Color.Parse("#272C33")), 1);
        var major = new Pen(new SolidColorBrush(Color.Parse("#3A424C")), 1.25);
        for (int i = -20; i <= 20; i++)
        {
            var p1 = Project(new Vector3(i, 0, -20)); var p2 = Project(new Vector3(i, 0, 20));
            var q1 = Project(new Vector3(-20, 0, i)); var q2 = Project(new Vector3(20, 0, i));
            dc.DrawLine(i % 5 == 0 ? major : minor, p1, p2); dc.DrawLine(i % 5 == 0 ? major : minor, q1, q2);
        }
    }

    private void DrawMesh(DrawingContext dc, SceneObject obj, bool selected)
    {
        var mesh = obj.Mesh;
        var faceBrush = new SolidColorBrush(selected ? Color.Parse("#70502A") : Color.Parse("#343C47"), .72);
        var edgePen = new Pen(new SolidColorBrush(selected ? Color.Parse("#FFB04A") : Color.Parse("#9BA8B7")), selected ? 2 : 1);
        var ordered = mesh.Faces.Select((f, i) => (f, i, z: f.Indices.Average(v => Camera(mesh.Vertices[v]).Z))).OrderBy(x => x.z);
        foreach (var item in ordered)
        {
            var points = item.f.Indices.Select(i => Project(mesh.Vertices[i])).ToArray();
            if (points.Length >= 3)
            {
                var geo = new StreamGeometry();
                using var g = geo.Open(); g.BeginFigure(points[0], true); for (int i = 1; i < points.Length; i++) g.LineTo(points[i]); g.EndFigure(true);
                dc.DrawGeometry(faceBrush, null, geo);
            }
        }
        foreach (var e in mesh.Edges) dc.DrawLine(edgePen, Project(mesh.Vertices[e.A]), Project(mesh.Vertices[e.B]));
        if (selected && mesh.SelectionMode == MeshSelectionMode.Vertex)
        {
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var p = Project(mesh.Vertices[i]); var b = mesh.SelectedVertices.Contains(i) ? Brushes.Orange : Brushes.White;
                dc.DrawEllipse(b, null, p, 3.5, 3.5);
            }
        }
    }

    private Vector3 Camera(Vector3 v)
    {
        var cy = (float)Math.Cos(_yaw); var sy = (float)Math.Sin(_yaw); var cp = (float)Math.Cos(_pitch); var sp = (float)Math.Sin(_pitch);
        var x = v.X * cy - v.Z * sy; var z = v.X * sy + v.Z * cy; var y = v.Y * cp - z * sp; z = v.Y * sp + z * cp;
        return new(x, y, z);
    }

    private Point Project(Vector3 v)
    {
        var c = Camera(v); var scale = _zoom / Math.Max(0.3, 1 + c.Z * .035);
        return new Point(Bounds.Width / 2 + _pan.X + c.X * scale, Bounds.Height / 2 + _pan.Y - c.Y * scale);
    }

    private void DrawAxis(DrawingContext dc)
    {
        var o = new Point(55, Bounds.Height - 55);
        dc.DrawLine(new Pen(Brushes.IndianRed, 2), o, o + new Avalonia.Vector(30, 0));
        dc.DrawLine(new Pen(Brushes.LightGreen, 2), o, o + new Avalonia.Vector(0, -30));
        dc.DrawLine(new Pen(Brushes.SkyBlue, 2), o, o + new Avalonia.Vector(-18, 18));
    }

    private void DrawOverlay(DrawingContext dc)
    {
        var mode = _state.Selected?.Mesh.SelectionMode.ToString() ?? "Object";
        dc.DrawText(new FormattedText($"{_state.Workspace}  |  {mode} Mode", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 13, Brushes.White), new Point(12, 10));
        dc.DrawText(new FormattedText("MMB orbit • Shift+MMB pan • Wheel zoom", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Gray), new Point(12, Bounds.Height - 26));
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus(); _last = e.GetPosition(this); var props = e.GetCurrentPoint(this).Properties;
        _orbiting = props.IsMiddleButtonPressed && !e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        _panning = props.IsMiddleButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (props.IsLeftButtonPressed) SelectNearestVertex(_last);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        var p = e.GetPosition(this); var d = p - _last; _last = p;
        if (_orbiting) { _yaw += d.X * .008; _pitch = Math.Clamp(_pitch + d.Y * .008, -1.45, 1.45); InvalidateVisual(); }
        if (_panning) { _pan += new Vector2((float)d.X, (float)d.Y); InvalidateVisual(); }
    }

    private void SelectNearestVertex(Point click)
    {
        var mesh = _state.Selected?.Mesh; if (mesh is null || mesh.SelectionMode != MeshSelectionMode.Vertex) return;
        var nearest = mesh.Vertices.Select((v, i) => (i, d: Distance(Project(v), click))).OrderBy(x => x.d).FirstOrDefault();
        if (nearest.d <= 12) { mesh.SelectedVertices.Clear(); mesh.SelectedVertices.Add(nearest.i); _state.Notify(); }
    }

    private static double Distance(Point a, Point b) { var x = a.X - b.X; var y = a.Y - b.Y; return Math.Sqrt(x * x + y * y); }
}
