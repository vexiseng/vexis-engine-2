using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Vexis.Editor.Desktop;

public sealed class World3DWindow : Window
{
    private readonly Terrain3DViewport _viewport;

    public World3DWindow(EditorState state)
    {
        Title = "Vexis Studio — Live 3D World Preview";
        Width = 1100;
        Height = 760;
        MinWidth = 640;
        MinHeight = 420;
        Background = new SolidColorBrush(Color.Parse("#182028"));
        _viewport = new Terrain3DViewport(state);
        Content = BuildContent();
    }

    private Control BuildContent()
    {
        var root = new DockPanel();
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(8, 6)
        };

        var reset = new Button { Content = "Reset Camera" };
        reset.Click += (_, _) => _viewport.ResetCamera();
        toolbar.Children.Add(reset);

        var wireframe = new CheckBox { Content = "Wireframe", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
        wireframe.IsCheckedChanged += (_, _) => { _viewport.ShowWireframe = wireframe.IsChecked == true; _viewport.InvalidateVisual(); };
        toolbar.Children.Add(wireframe);

        var objects = new CheckBox { Content = "Objects", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
        objects.IsCheckedChanged += (_, _) => { _viewport.ShowObjects = objects.IsChecked == true; _viewport.InvalidateVisual(); };
        toolbar.Children.Add(objects);

        toolbar.Children.Add(new TextBlock
        {
            Text = "Updates instantly while you sculpt in the main editor",
            Foreground = Brushes.LightGray,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        });

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#56616D")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = toolbar
        };
        DockPanel.SetDock(border, Dock.Top);
        root.Children.Add(border);
        root.Children.Add(_viewport);
        return root;
    }
}
