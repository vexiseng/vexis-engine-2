using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Vexis.Editor.Desktop;

public sealed class MainWindow : Window
{
    private const string ProjectPath = "Vaelor.vexis.json";
    private readonly EditorState _state = new();
    private readonly ContentControl _center = new();
    private readonly ListBox _outliner = new();
    private readonly ListBox _contentList = new();
    private readonly StackPanel _inspector = new() { Spacing = 8, Margin = new Thickness(12) };
    private readonly ListBox _console = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _workspaceTitle = new();
    private readonly TerrainViewport _terrain;
    private readonly WorldMapView _worldMap = new();
    private readonly TextBox _aiPrompt = new() { PlaceholderText = "Describe a world or content change…" };
    private World3DWindow? _world3DWindow;

    public MainWindow()
    {
        _terrain = new TerrainViewport(_state);
        Title = "Vexis Studio 2 — Vaelor";
        Width = 1500;
        Height = 900;
        MinWidth = 1050;
        MinHeight = 700;
        Background = new SolidColorBrush(Color.Parse("#242A31"));
        Foreground = new SolidColorBrush(Color.Parse("#F2F5F7"));
        Content = BuildShell();
        _state.Changed += Refresh;
        Closing += (_, _) => { if (_state.IsDirty) TrySave(); };
        KeyDown += OnKeyDown;
        SwitchWorkspace("World");
        Refresh();
    }

    private Control BuildShell()
    {
        var root = new DockPanel();
        var menu = BuildMenu(); DockPanel.SetDock(menu, Dock.Top); root.Children.Add(menu);
        var toolbar = BuildToolbar(); DockPanel.SetDock(toolbar, Dock.Top); root.Children.Add(toolbar);
        var status = BuildStatus(); DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        root.Children.Add(BuildWorkspace());
        return root;
    }

    private Control BuildMenu()
    {
        return new Menu
        {
            ItemsSource = new object[]
            {
                new MenuItem { Header = "_File", ItemsSource = new object[]
                {
                    MenuAction("Save Project", TrySave),
                    MenuAction("Load Project", TryLoad),
                    new Separator(),
                    MenuAction("Build Vaelor", () => Log("Build pipeline queued (runtime packaging milestone).")),
                    MenuAction("Exit", Close)
                }},
                new MenuItem { Header = "_Edit", ItemsSource = new object[]
                {
                    MenuAction("Undo", () => Log("Undo command requested.")),
                    MenuAction("Redo", () => Log("Redo command requested.")),
                    MenuAction("Duplicate Selected", _state.DuplicateSelected),
                    new Separator(),
                    MenuAction("Preferences", () => Log("Preferences workspace selected."))
                }},
                new MenuItem { Header = "_World", ItemsSource = new object[]
                {
                    MenuAction("Terrain Editor", () => SwitchWorkspace("World")),
                    MenuAction("World Map", () => SwitchWorkspace("World Map")),
                    MenuAction("Live 3D World Preview", Open3DPreview),
                    MenuAction("Place Tree", () => _state.AddWorldObject("Oak Tree", "Scenery", 28, 30)),
                    MenuAction("Place NPC Spawn", () => _state.AddWorldObject("NPC Spawn", "Gameplay", 34, 32)),
                    MenuAction("Water Body", () => Log("Semantic water-body tool selected."))
                }},
                new MenuItem { Header = "_Content", ItemsSource = new object[]
                {
                    MenuAction("NPC Editor", () => SwitchWorkspace("NPCs")),
                    MenuAction("Item Editor", () => SwitchWorkspace("Items")),
                    MenuAction("Quest Editor", () => SwitchWorkspace("Quests")),
                    MenuAction("Dialogue Editor", () => SwitchWorkspace("Dialogue")),
                    MenuAction("Spawn Editor", () => SwitchWorkspace("Spawns")),
                    MenuAction("Shop Editor", () => SwitchWorkspace("Shops")),
                    MenuAction("Loot Editor", () => SwitchWorkspace("Loot")),
                    MenuAction("Ability Editor", () => SwitchWorkspace("Abilities")),
                    MenuAction("Skill Editor", () => SwitchWorkspace("Skills"))
                }},
                new MenuItem { Header = "_Run", ItemsSource = new object[]
                {
                    MenuAction("Play", Play), MenuAction("Pause", Pause), MenuAction("Stop", Stop)
                }},
                new MenuItem { Header = "_Help", ItemsSource = new object[]
                {
                    MenuAction("Implementation Status", () => Log("Milestone 1: functional world/content editor vertical slice."))
                }}
            }
        };
    }

    private MenuItem MenuAction(string text, Action action)
    {
        var item = new MenuItem { Header = text };
        item.Click += (_, _) => action();
        return item;
    }

    private Control BuildToolbar()
    {
        var bar = new DockPanel { Margin = new Thickness(8, 6) };
        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        DockPanel.SetDock(right, Dock.Right);
        right.Children.Add(ToolButton("▶ Play", Play));
        right.Children.Add(ToolButton("Ⅱ", Pause));
        right.Children.Add(ToolButton("■", Stop));
        right.Children.Add(ToolButton("Save", TrySave));
        bar.Children.Add(right);

        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        left.Children.Add(ToolButton("Project", () => SwitchWorkspace("Project")));
        left.Children.Add(ToolButton("World", () => SwitchWorkspace("World")));
        left.Children.Add(ToolButton("Map", () => SwitchWorkspace("World Map")));
        left.Children.Add(ToolButton("3D Preview", Open3DPreview));
        left.Children.Add(ToolButton("NPCs", () => SwitchWorkspace("NPCs")));
        left.Children.Add(ToolButton("Items", () => SwitchWorkspace("Items")));
        left.Children.Add(ToolButton("Quests", () => SwitchWorkspace("Quests")));
        left.Children.Add(ToolButton("Dialogue", () => SwitchWorkspace("Dialogue")));
        left.Children.Add(SeparatorLine());
        left.Children.Add(ToolButton("Raise", () => SetBrush(TerrainBrushMode.Raise)));
        left.Children.Add(ToolButton("Lower", () => SetBrush(TerrainBrushMode.Lower)));
        left.Children.Add(ToolButton("Smooth", () => SetBrush(TerrainBrushMode.Smooth)));
        left.Children.Add(ToolButton("Flatten", () => SetBrush(TerrainBrushMode.Flatten)));
        bar.Children.Add(left);
        return new Border { BorderBrush = new SolidColorBrush(Color.Parse("#56616D")), BorderThickness = new Thickness(0, 0, 0, 1), Child = bar };
    }

    private static Control SeparatorLine() => new Border { Width = 1, Height = 25, Margin = new Thickness(5, 0), Background = Brushes.DimGray };

    private static Button ToolButton(string text, Action action)
    {
        var button = new Button { Content = text, Padding = new Thickness(11, 5) };
        button.Click += (_, _) => action();
        return button;
    }

    private Control BuildWorkspace()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(260, GridUnitType.Pixel));
        grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(310, GridUnitType.Pixel));
        grid.RowDefinitions.Add(new RowDefinition(42, GridUnitType.Pixel));
        grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
        grid.RowDefinitions.Add(new RowDefinition(210, GridUnitType.Pixel));

        var left = BuildLeftPanel(); Grid.SetColumn(left, 0); Grid.SetRowSpan(left, 3); grid.Children.Add(left);
        var titleBar = BuildWorkspaceTitle(); Grid.SetColumn(titleBar, 1); Grid.SetRow(titleBar, 0); grid.Children.Add(titleBar);
        Grid.SetColumn(_center, 1); Grid.SetRow(_center, 1); grid.Children.Add(_center);
        var right = BuildRightPanel(); Grid.SetColumn(right, 2); Grid.SetRowSpan(right, 3); grid.Children.Add(right);
        var bottom = BuildBottomPanel(); Grid.SetColumn(bottom, 1); Grid.SetRow(bottom, 2); grid.Children.Add(bottom);
        return grid;
    }

    private Control BuildWorkspaceTitle()
    {
        _workspaceTitle.FontSize = 18;
        _workspaceTitle.FontWeight = FontWeight.SemiBold;
        _workspaceTitle.VerticalAlignment = VerticalAlignment.Center;
        _workspaceTitle.Margin = new Thickness(12, 0);
        return new Border { Background = new SolidColorBrush(Color.Parse("#303841")), BorderBrush = new SolidColorBrush(Color.Parse("#56616D")), BorderThickness = new Thickness(0, 0, 0, 1), Child = _workspaceTitle };
    }

    private Control BuildLeftPanel()
    {
        _outliner.SelectionChanged += (_, _) =>
        {
            _state.Selected = _outliner.SelectedItem as SceneObject;
            _state.SelectedContent = null;
            RefreshInspector();
            _center.InvalidateVisual();
        };
        _contentList.SelectionChanged += (_, _) =>
        {
            _state.SelectedContent = _contentList.SelectedItem as ContentDefinition;
            _state.Selected = null;
            RefreshInspector();
        };

        var tabs = new TabControl
        {
            ItemsSource = new object[]
            {
                new TabItem { Header = "World", Foreground = Brushes.White, Content = BuildListPanel("WORLD OUTLINER", _outliner) },
                new TabItem { Header = "Content", Foreground = Brushes.White, Content = BuildListPanel("CONTENT DATABASE", _contentList) },
                new TabItem { Header = "Assets", Foreground = Brushes.White, Content = BuildAssetBrowser() }
            }
        };
        return PanelBorder(tabs);
    }

    private static Control BuildListPanel(string title, Control list)
    {
        var panel = new DockPanel();
        var header = Header(title); DockPanel.SetDock(header, Dock.Top); panel.Children.Add(header);
        panel.Children.Add(list);
        return panel;
    }

    private static Control BuildAssetBrowser()
    {
        var panel = new StackPanel { Margin = new Thickness(10), Spacing = 6 };
        panel.Children.Add(new TextBox { PlaceholderText = "Search assets…" });
        foreach (var folder in new[] { "Models", "Materials", "Textures", "Animations", "Audio", "UI", "Worlds", "Data" })
            panel.Children.Add(new Button { Content = $"▸  {folder}/", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left });
        panel.Children.Add(new TextBlock { Text = "Blender assets will enter through glTF/GLB import and automatic reimport.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), Foreground = Brushes.LightGray });
        return panel;
    }

    private Control BuildRightPanel()
    {
        var scroll = new ScrollViewer { Content = _inspector };
        var panel = new DockPanel();
        var header = Header("INSPECTOR / TOOL PROPERTIES"); DockPanel.SetDock(header, Dock.Top); panel.Children.Add(header);
        panel.Children.Add(scroll);
        return PanelBorder(panel);
    }

    private Control BuildBottomPanel()
    {
        _console.ItemsSource = _state.Log;
        var tabs = new TabControl
        {
            ItemsSource = new object[]
            {
                new TabItem { Header = "Console", Foreground = Brushes.White, Content = _console },
                new TabItem { Header = "AI Assistant", Foreground = Brushes.White, Content = BuildAiPanel() },
                new TabItem { Header = "Validation", Foreground = Brushes.White, Content = new TextBlock { Text = "Project validation will report broken references, missing assets, unreachable quests, bad spawns, and build blockers.", Margin = new Thickness(12), TextWrapping = TextWrapping.Wrap } },
                new TabItem { Header = "Build Tasks", Foreground = Brushes.White, Content = new TextBlock { Text = "Asset imports, terrain baking, navmesh generation, map tiles, and runtime packaging will appear here.", Margin = new Thickness(12), TextWrapping = TextWrapping.Wrap } }
            }
        };
        return PanelBorder(tabs);
    }

    private Control BuildAiPanel()
    {
        var panel = new DockPanel { Margin = new Thickness(10) };
        var send = new Button { Content = "Plan & Preview", Margin = new Thickness(7, 0, 0, 0) };
        send.Click += (_, _) =>
        {
            var prompt = _aiPrompt.Text?.Trim();
            if (string.IsNullOrWhiteSpace(prompt)) return;
            Log($"AI request: {prompt}");
            Log("Created a reviewable command-plan placeholder; Ollama execution wiring remains in the AI project.");
            _aiPrompt.Text = string.Empty;
        };
        DockPanel.SetDock(send, Dock.Right); panel.Children.Add(send); panel.Children.Add(_aiPrompt);
        return panel;
    }

    private Control BuildStatus()
    {
        _status.Margin = new Thickness(9, 4);
        return new Border { BorderBrush = new SolidColorBrush(Color.Parse("#56616D")), BorderThickness = new Thickness(0, 1, 0, 0), Child = _status };
    }

    private void Refresh()
    {
        _outliner.ItemsSource = null; _outliner.ItemsSource = _state.Objects; _outliner.SelectedItem = _state.Selected;
        _contentList.ItemsSource = null; _contentList.ItemsSource = FilteredContent(); _contentList.SelectedItem = _state.SelectedContent;
        _console.ItemsSource = null; _console.ItemsSource = _state.Log;
        _workspaceTitle.Text = $"{_state.Workspace.ToUpperInvariant()}  —  {_state.ProjectName}{(_state.IsDirty ? "  •  UNSAVED" : string.Empty)}";
        _status.Text = $"Workspace: {_state.Workspace}   World objects: {_state.Objects.Count}   Content records: {_state.Content.Count}   Terrain rev: {_state.Terrain.Revision}   Runtime: {RuntimeStatus()}";
        RefreshInspector();
        _center.InvalidateVisual();
    }

    private IEnumerable<ContentDefinition> FilteredContent()
    {
        return _state.Workspace switch
        {
            "NPCs" => _state.Content.Where(x => x.Type == "npc"),
            "Items" => _state.Content.Where(x => x.Type == "item"),
            "Quests" => _state.Content.Where(x => x.Type == "quest"),
            "Dialogue" => _state.Content.Where(x => x.Type == "dialogue"),
            "Spawns" => _state.Content.Where(x => x.Type == "spawn"),
            "Shops" => _state.Content.Where(x => x.Type == "shop"),
            "Loot" => _state.Content.Where(x => x.Type == "loot"),
            "Abilities" => _state.Content.Where(x => x.Type == "ability"),
            "Skills" => _state.Content.Where(x => x.Type == "skill"),
            _ => _state.Content
        };
    }

    private void RefreshInspector()
    {
        _inspector.Children.Clear();
        if (_state.SelectedContent is not null) { BuildContentInspector(_state.SelectedContent); return; }
        if (_state.Selected is not null) { BuildObjectInspector(_state.Selected); return; }
        BuildToolInspector();
    }

    private void BuildToolInspector()
    {
        _inspector.Children.Add(new TextBlock { Text = "Terrain Brush", FontSize = 18, FontWeight = FontWeight.SemiBold });
        _inspector.Children.Add(new TextBlock { Text = "Mode" });
        var mode = new ComboBox { ItemsSource = Enum.GetValues<TerrainBrushMode>(), SelectedItem = _terrain.BrushMode };
        mode.SelectionChanged += (_, _) => { if (mode.SelectedItem is TerrainBrushMode value) SetBrush(value); };
        _inspector.Children.Add(mode);
        _inspector.Children.Add(new TextBlock { Text = $"Radius: {_terrain.BrushRadius:0.0}" });
        var radius = new Slider { Minimum = 1, Maximum = 20, Value = _terrain.BrushRadius };
        radius.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) { _terrain.BrushRadius = (float)radius.Value; _terrain.InvalidateVisual(); } };
        _inspector.Children.Add(radius);
        _inspector.Children.Add(new TextBlock { Text = $"Strength: {_terrain.BrushStrength:0.00}" });
        var strength = new Slider { Minimum = .02, Maximum = 1, Value = _terrain.BrushStrength };
        strength.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) _terrain.BrushStrength = (float)strength.Value; };
        _inspector.Children.Add(strength);
        _inspector.Children.Add(new Separator());
        _inspector.Children.Add(ActionButton("Place Oak Tree", () => _state.AddWorldObject("Oak Tree", "Scenery", 30, 30)));
        _inspector.Children.Add(ActionButton("Place NPC Spawn", () => _state.AddWorldObject("NPC Spawn", "Gameplay", 34, 32)));
        _inspector.Children.Add(ActionButton("Create Water Body", () => Log("Water solve preview queued for selected basin.")));
        _inspector.Children.Add(new Separator());
        _inspector.Children.Add(ActionButton("Generate Rolling Terrain", () => { _state.Terrain.GenerateRollingTerrain(Environment.TickCount); _state.MarkDirty(); _terrain.InvalidateVisual(); Log("Generated a new rolling terrain seed."); }));
        _inspector.Children.Add(ActionButton("Flatten Entire Terrain", () => { _state.Terrain.FlattenAll(); _state.MarkDirty(); _terrain.InvalidateVisual(); Log("Flattened the terrain heightfield."); }));
    }

    private void BuildObjectInspector(SceneObject obj)
    {
        _inspector.Children.Add(new TextBlock { Text = obj.Name, FontSize = 18, FontWeight = FontWeight.SemiBold });
        _inspector.Children.Add(LabeledTextBox("Name", obj.Name, value => obj.Name = value));
        _inspector.Children.Add(LabeledTextBox("Category", obj.Category, value => obj.Category = value));
        _inspector.Children.Add(VectorEditor("Position", obj.Position, value => obj.Position = value));
        _inspector.Children.Add(VectorEditor("Rotation", obj.Rotation, value => obj.Rotation = value));
        _inspector.Children.Add(VectorEditor("Scale", obj.Scale, value => obj.Scale = value));
        _inspector.Children.Add(new Separator());
        _inspector.Children.Add(ActionButton("Move +X", () => MoveObject(obj, 1, 0)));
        _inspector.Children.Add(ActionButton("Move -X", () => MoveObject(obj, -1, 0)));
        _inspector.Children.Add(ActionButton("Move +Z", () => MoveObject(obj, 0, 1)));
        _inspector.Children.Add(ActionButton("Move -Z", () => MoveObject(obj, 0, -1)));
        _inspector.Children.Add(ActionButton("Snap to Terrain", () => Log($"{obj.Name} snapped to terrain surface.")));
        _inspector.Children.Add(ActionButton("Duplicate Object", _state.DuplicateSelected));
        _inspector.Children.Add(ActionButton("Delete Object", _state.DeleteSelected));
    }

    private void BuildContentInspector(ContentDefinition content)
    {
        _inspector.Children.Add(new TextBlock { Text = content.Name, FontSize = 18, FontWeight = FontWeight.SemiBold });
        _inspector.Children.Add(new TextBlock { Text = content.Type.ToUpperInvariant(), Foreground = Brushes.Orange });
        _inspector.Children.Add(LabeledTextBox("ID", content.Id, value => content.Id = value));
        _inspector.Children.Add(LabeledTextBox("Display Name", content.Name, value => content.Name = value));
        _inspector.Children.Add(LabeledTextBox("Description", content.Description, value => content.Description = value, true));
        _inspector.Children.Add(LabeledTextBox("Recommended Level", content.Level.ToString(), value => { if (int.TryParse(value, out var level)) content.Level = Math.Max(1, level); }));
        _inspector.Children.Add(new Separator());
        _inspector.Children.Add(new TextBlock { Text = "References and validation will appear here so every quest, NPC, item, spawn, dialogue, and asset remains connected.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray });
    }

    private Control LabeledTextBox(string label, string value, Action<string> commit, bool multiline = false)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label });
        var box = new TextBox { Text = value, AcceptsReturn = multiline, MinHeight = multiline ? 72 : 0, TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap };
        box.LostFocus += (_, _) => { commit(box.Text ?? string.Empty); _state.MarkDirty(); };
        panel.Children.Add(box);
        return panel;
    }

    private static Button ActionButton(string text, Action action)
    {
        var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => action();
        return button;
    }

    private void SwitchWorkspace(string name)
    {
        _state.Workspace = name;
        _state.SelectedContent = null;
        _center.Content = name switch
        {
            "Project" => BuildProjectDashboard(),
            "World Map" => _worldMap,
            "World" => _terrain,
            "NPCs" => BuildContentWorkspace("NPC", "npc"),
            "Items" => BuildContentWorkspace("Item", "item"),
            "Quests" => BuildContentWorkspace("Quest", "quest"),
            "Dialogue" => BuildContentWorkspace("Dialogue", "dialogue"),
            "Spawns" => BuildContentWorkspace("Spawn", "spawn"),
            "Shops" => BuildContentWorkspace("Shop", "shop"),
            "Loot" => BuildContentWorkspace("Loot Table", "loot"),
            "Abilities" => BuildContentWorkspace("Ability", "ability"),
            "Skills" => BuildContentWorkspace("Skill", "skill"),
            _ => _terrain
        };
        Log($"Switched to {name} workspace");
        _state.Notify();
    }

    private Control BuildContentWorkspace(string displayType, string type)
    {
        var root = new DockPanel { Margin = new Thickness(18) };
        var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 12) };
        top.Children.Add(new TextBlock { Text = $"{displayType} Database", FontSize = 24, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        top.Children.Add(ToolButton($"+ New {displayType}", () => _state.AddContent(type)));
        DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
        var list = new ListBox { ItemsSource = _state.Content.Where(x => x.Type == type).ToList() };
        list.SelectionChanged += (_, _) => { _state.SelectedContent = list.SelectedItem as ContentDefinition; _state.Selected = null; RefreshInspector(); };
        root.Children.Add(list);
        return root;
    }

    private Control BuildProjectDashboard()
    {
        var root = new StackPanel { Margin = new Thickness(24), Spacing = 14 };
        root.Children.Add(new TextBlock { Text = "Vaelor Project Dashboard", FontSize = 28, FontWeight = FontWeight.Bold });
        root.Children.Add(new TextBlock { Text = "Central access to world building, RPG databases, validation, runtime testing, and asset processing.", Foreground = Brushes.LightGray });
        var grid = new UniformGrid { Columns = 3, Rows = 2 };
        grid.Children.Add(DashboardCard("WORLD", $"{_state.Objects.Count} placed objects\nTerrain revision {_state.Terrain.Revision}", () => SwitchWorkspace("World")));
        grid.Children.Add(DashboardCard("RPG CONTENT", $"{_state.Content.Count} definitions\nNPCs, items, quests and more", () => SwitchWorkspace("NPCs")));
        grid.Children.Add(DashboardCard("WORLD MAP", "Generated map layers, labels, icons and travel links", () => SwitchWorkspace("World Map")));
        grid.Children.Add(DashboardCard("ASSET PIPELINE", "GLB import, reimport, materials, collision and thumbnails", () => Log("Asset pipeline workspace is queued next.")));
        grid.Children.Add(DashboardCard("VALIDATION", "Cross-reference checks and build blockers", () => Log("Validation scan requested.")));
        grid.Children.Add(DashboardCard("PLAY & DEBUG", RuntimeStatus(), Play));
        root.Children.Add(grid);
        return new ScrollViewer { Content = root };
    }

    private static Border DashboardCard(string title, string body, Action action)
    {
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White });
        panel.Children.Add(new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray });
        var open = new Button { Content = "Open", HorizontalAlignment = HorizontalAlignment.Left };
        open.Click += (_, _) => action();
        panel.Children.Add(open);
        return new Border { Margin = new Thickness(6), Padding = new Thickness(4), MinHeight = 135, Background = new SolidColorBrush(Color.Parse("#303841")), BorderBrush = new SolidColorBrush(Color.Parse("#56616D")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Child = panel };
    }

    private Control VectorEditor(string label, System.Numerics.Vector3 value, Action<System.Numerics.Vector3> commit)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeight.SemiBold });
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        var boxes = new[] { new TextBox { Text = value.X.ToString("0.###") }, new TextBox { Text = value.Y.ToString("0.###") }, new TextBox { Text = value.Z.ToString("0.###") } };
        for (var i = 0; i < boxes.Length; i++) { Grid.SetColumn(boxes[i], i); boxes[i].Margin = new Thickness(i == 0 ? 0 : 3, 0, 0, 0); row.Children.Add(boxes[i]); }
        void SaveVector()
        {
            if (float.TryParse(boxes[0].Text, out var x) && float.TryParse(boxes[1].Text, out var y) && float.TryParse(boxes[2].Text, out var z))
            {
                commit(new System.Numerics.Vector3(x, y, z));
                _state.MarkDirty();
            }
        }
        foreach (var box in boxes) box.LostFocus += (_, _) => SaveVector();
        panel.Children.Add(row);
        return panel;
    }

    private void SetBrush(TerrainBrushMode mode)
    {
        _terrain.BrushMode = mode;
        _state.Selected = null;
        _state.SelectedContent = null;
        Log($"Terrain brush: {mode}");
        RefreshInspector();
        _terrain.InvalidateVisual();
    }

    private void MoveObject(SceneObject obj, float dx, float dz)
    {
        obj.Position += new System.Numerics.Vector3(dx, 0, dz);
        _state.MarkDirty();
    }


    private void Open3DPreview()
    {
        if (_world3DWindow is { IsVisible: true })
        {
            _world3DWindow.Activate();
            return;
        }

        _world3DWindow = new World3DWindow(_state);
        _world3DWindow.Closed += (_, _) => _world3DWindow = null;
        _world3DWindow.Show(this);
        Log("Opened live 3D world preview.");
    }

    private void Play()
    {
        _state.Runtime.SceneMeshes.Clear();
        _state.Runtime.SceneMeshes.AddRange(_state.Objects.Select(o => o.Mesh.Clone()));
        _state.Runtime.Play();
        Log("Play-in-editor started from an isolated world snapshot.");
        _state.Notify();
    }

    private void Pause() { _state.Runtime.Pause(); Log("Runtime pause toggled."); _state.Notify(); }
    private void Stop() { _state.Runtime.Stop(); Log("Runtime stopped; editor world preserved."); _state.Notify(); }

    private void TrySave()
    {
        try { _state.Save(ProjectPath); }
        catch (Exception ex) { Log($"Save failed: {ex.Message}"); }
    }

    private void TryLoad()
    {
        try
        {
            if (!File.Exists(ProjectPath)) { Log($"No {ProjectPath} file exists yet. Save first."); return; }
            _state.Load(ProjectPath);
            if (_state.Workspace == "World") _center.Content = _terrain;
        }
        catch (Exception ex) { Log($"Load failed: {ex.Message}"); }
    }

    private string RuntimeStatus() => _state.Runtime.IsPlaying ? (_state.Runtime.IsPaused ? "Paused" : "Playing") : "Stopped";
    private void Log(string text) { _state.Log.Add($"[{DateTime.Now:HH:mm:ss}] {text}"); _state.Notify(); }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S) { TrySave(); e.Handled = true; }
        if (e.Key == Key.Delete && _state.Selected is not null) _state.DeleteSelected();
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.D && _state.Selected is not null) _state.DuplicateSelected();
        if (e.Key == Key.F5) Play();
        if (e.Key == Key.Escape && _state.Runtime.IsPlaying) Stop();
    }

    private static Border PanelBorder(Control child) => new() { Background = new SolidColorBrush(Color.Parse("#2B323A")), BorderBrush = new SolidColorBrush(Color.Parse("#56616D")), BorderThickness = new Thickness(1), Child = child };
    private static TextBlock Header(string text) => new() { Text = text, FontWeight = FontWeight.SemiBold, Margin = new Thickness(9, 7), Foreground = Brushes.LightGray };
}
