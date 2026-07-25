using System.Collections.ObjectModel;
using System.Text.Json;
using Vexis.Modeling;
using Vexis.Runtime;
using Vexis.World;

namespace Vexis.Editor.Desktop;

public sealed class EditorState
{
    public ObservableCollection<SceneObject> Objects { get; } = [];
    public ObservableCollection<string> Log { get; } = [];
    public ObservableCollection<ContentDefinition> Content { get; } = [];
    public RuntimeSession Runtime { get; } = new();
    public EditorHistory History { get; } = new();
    public TerrainBrushPreview BrushPreview { get; } = new();
    public TerrainDocument Terrain { get; private set; } = new(64, 64);
    public WaterBodyDefinition? ActiveWaterBody { get; private set; }
    public SolvedWaterBody? WaterPreview { get; private set; }
    public List<WaterBodyDefinition> WaterBodies { get; } = [];
    public IReadOnlyDictionary<Guid, SolvedWaterBody> SolvedWaterBodies => _solvedWaterBodies;
    private readonly Dictionary<Guid, SolvedWaterBody> _solvedWaterBodies = [];
    private float[]? _terrainStrokeBefore;
    public List<AssetRecord> Assets { get; } = [];
    public IReadOnlyList<ContentReference> ContentReferences { get; private set; } = [];
    public IReadOnlyList<ValidationIssue> ValidationIssues { get; private set; } = [];
    public IReadOnlyList<BuildTask> BuildTasks { get; private set; } = [];
    public RuntimeLaunchState RuntimeLaunchState { get; private set; } = new("Ready", "Studio is ready to launch.");
    public SceneObject? Selected { get; set; }
    public ContentDefinition? SelectedContent { get; set; }
    public string Workspace { get; set; } = "World";
    public string ProjectName { get; set; } = "Vaelor";
    public bool IsDirty { get; private set; }
    public event Action? Changed;

    public EditorState()
    {
        AddMesh(MeshFactory.Cube(), "Player Spawn Marker", notify: false);
        Objects[0].Category = "Gameplay";
        Objects[0].Position = new(32, 0, 32);
        Content.Add(new ContentDefinition("npc", "town_guard", "Town Guard"));
        Content.Add(new ContentDefinition("item", "bronze_sword", "Bronze Sword"));
        Content.Add(new ContentDefinition("quest", "first_steps", "First Steps"));
        Content.Add(new ContentDefinition("dialogue", "guard_greeting", "Guard Greeting"));
        Content.Add(new ContentDefinition("spawn", "lumbridge_guard_spawn", "Town Guard Spawn"));
        Content.Add(new ContentDefinition("shop", "general_store", "General Store"));
        Content.Add(new ContentDefinition("loot", "goblin_common", "Goblin Common Drops"));
        Content.Add(new ContentDefinition("ability", "power_strike", "Power Strike"));
        Content.Add(new ContentDefinition("skill", "woodcutting", "Woodcutting"));
        Log.Add("Vexis Studio ready — World workspace loaded.");
    }

    public void AddMesh(MeshDocument mesh, string? name = null, bool notify = true)
    {
        var o = new SceneObject(name ?? mesh.Name, mesh);
        Objects.Add(o);
        Selected = o;
        Log.Add($"Created {o.Name}");
        if (notify) MarkDirty();
    }

    public void AddWorldObject(string name, string category, float x, float z)
    {
        var o = new SceneObject(name, MeshFactory.Cube(.35f))
        {
            Category = category,
            Position = new(x, 0, z)
        };
        Objects.Add(o);
        Selected = o;
        Log.Add($"Placed {name} at ({x:0.0}, {z:0.0})");
        MarkDirty();
    }

    public void BuildWaterPreview(float surfaceElevation, int seedX, int seedZ)
    {
        if (seedX < 0 || seedZ < 0 || seedX >= Terrain.Width - 1 || seedZ >= Terrain.Height - 1)
            throw new ArgumentOutOfRangeException(nameof(seedX), "The water seed must be inside the terrain.");

        var definition = new WaterBodyDefinition(
            Guid.NewGuid(),
            $"Lake {WaterBodies.Count + 1}",
            surfaceElevation,
            [new WorldCell(seedX, seedZ)],
            new WaterSolveBounds(0, 0, Terrain.Width - 2, Terrain.Height - 2));

        ActiveWaterBody = definition;
        WaterPreview = SolveWater(definition);
        Log.Add(WaterPreview.Cells.Count == 0
            ? "Water preview is empty. Raise its elevation or choose a lower seed."
            : $"Previewing {WaterPreview.Cells.Count} water cell(s) at elevation {surfaceElevation:0.00}. Confirm or cancel the preview.");
        Notify();
    }

    public bool CommitWaterPreview()
    {
        if (ActiveWaterBody is null || WaterPreview is null || WaterPreview.Cells.Count == 0)
            return false;

        var definition = ActiveWaterBody;
        var solved = WaterPreview;
        WaterBodies.Add(definition);
        _solvedWaterBodies[definition.Id] = solved;
        ClearWaterPreview();

        History.Record(new DelegateEditorOperation(
            $"Create {definition.Name}",
            () => { WaterBodies.RemoveAll(w => w.Id == definition.Id); _solvedWaterBodies.Remove(definition.Id); MarkDirty(); },
            () => { WaterBodies.Add(definition); _solvedWaterBodies[definition.Id] = solved; MarkDirty(); }));

        Log.Add($"Created {definition.Name} with {solved.Cells.Count} cell(s).");
        MarkDirty();
        return true;
    }

    public void CancelWaterPreview()
    {
        if (ActiveWaterBody is null && WaterPreview is null) return;
        ClearWaterPreview();
        Log.Add("Cancelled water preview.");
        Notify();
    }

    public void DeleteWaterBody(Guid id)
    {
        var definition = WaterBodies.FirstOrDefault(w => w.Id == id);
        if (definition is null) return;
        _solvedWaterBodies.TryGetValue(id, out var solved);
        var index = WaterBodies.IndexOf(definition);
        WaterBodies.RemoveAt(index);
        _solvedWaterBodies.Remove(id);
        History.Record(new DelegateEditorOperation(
            $"Delete {definition.Name}",
            () => { WaterBodies.Insert(Math.Min(index, WaterBodies.Count), definition); if (solved is not null) _solvedWaterBodies[id] = solved; MarkDirty(); },
            () => { WaterBodies.RemoveAll(w => w.Id == id); _solvedWaterBodies.Remove(id); MarkDirty(); }));
        MarkDirty();
    }

    private SolvedWaterBody SolveWater(WaterBodyDefinition definition)
    {
        var field = new GlobalElevationField { DefaultElevation = Terrain.CopyHeights().DefaultIfEmpty(0f).Average() };
        var heights = Terrain.CopyHeights();
        for (var z = 0; z < Terrain.Height; z++)
        for (var x = 0; x < Terrain.Width; x++)
            field.Set(new WorldVertex(x, z), heights[z * Terrain.Width + x]);
        return new WaterBodySolver(field).Solve(definition);
    }

    private void ClearWaterPreview()
    {
        ActiveWaterBody = null;
        WaterPreview = null;
    }

    public void BeginTerrainStroke()
    {
        _terrainStrokeBefore ??= Terrain.CopyHeights();
    }

    public void EndTerrainStroke(string description = "Sculpt terrain")
    {
        if (_terrainStrokeBefore is null) return;
        var before = _terrainStrokeBefore;
        _terrainStrokeBefore = null;
        var after = Terrain.CopyHeights();
        if (before.SequenceEqual(after)) return;
        History.Record(new DelegateEditorOperation(
            description,
            () => { Terrain.RestoreHeights(before); RebuildSolvedWaterBodies(); MarkDirty(); },
            () => { Terrain.RestoreHeights(after); RebuildSolvedWaterBodies(); MarkDirty(); }));
    }

    public void RecordTerrainChange(string description, float[] before)
    {
        var after = Terrain.CopyHeights();
        if (before.SequenceEqual(after)) return;
        History.Record(new DelegateEditorOperation(
            description,
            () => { Terrain.RestoreHeights(before); RebuildSolvedWaterBodies(); MarkDirty(); },
            () => { Terrain.RestoreHeights(after); RebuildSolvedWaterBodies(); MarkDirty(); }));
        MarkDirty();
    }

    public void Undo()
    {
        var description = History.UndoDescription;
        if (!History.Undo()) return;
        Log.Add($"Undid: {description}");
        Notify();
    }

    public void Redo()
    {
        var description = History.RedoDescription;
        if (!History.Redo()) return;
        Log.Add($"Redid: {description}");
        Notify();
    }

    private void RebuildSolvedWaterBodies()
    {
        _solvedWaterBodies.Clear();
        foreach (var body in WaterBodies)
            _solvedWaterBodies[body.Id] = SolveWater(body);
        if (ActiveWaterBody is not null) WaterPreview = SolveWater(ActiveWaterBody);
    }

    public void ValidateProject()
    {
        var validator = new ProjectValidationService();
        var graph = new ContentGraphService();
        var pipeline = new BuildPipelineService();
        var launch = new RuntimeLaunchService();
        ContentReferences = graph.BuildReferences(Content.Select(c => new ValidationContentItem(c.Type, c.Id)));
        ValidationIssues = validator.Validate(
            Terrain.Width,
            Terrain.Height,
            Objects.Select(o => new ValidationSceneObject(o.Name, o.Position.X, o.Position.Z)),
            Content.Select(c => new ValidationContentItem(c.Type, c.Id)),
            Assets.Select(a => new ValidationAsset(a.Name, a.Path, a.Kind)));
        BuildTasks = pipeline.CreatePlan(
            ValidationIssues,
            Assets,
            Terrain.Width > 0 && Terrain.Height > 0,
            Content.Count > 0);
        RuntimeLaunchState = launch.CreateState(
            Terrain.Width > 0 && Terrain.Height > 0,
            Assets.Count > 0,
            Content.Count > 0,
            ValidationIssues);
        Log.Add($"Validation completed with {ValidationIssues.Count} issue(s).");
        Notify();
    }

    public void DuplicateSelected()
    {
        if (Selected is null) return;
        var copy = new SceneObject($"{Selected.Name} Copy", Selected.Mesh.Clone())
        {
            Category = Selected.Category,
            Position = Selected.Position + new System.Numerics.Vector3(1, 0, 1),
            Rotation = Selected.Rotation,
            Scale = Selected.Scale
        };
        Objects.Add(copy);
        Selected = copy;
        Log.Add($"Duplicated {copy.Name}");
        MarkDirty();
    }

    public void DeleteSelected()
    {
        if (Selected is null) return;
        Log.Add($"Deleted {Selected.Name}");
        Objects.Remove(Selected);
        Selected = Objects.LastOrDefault();
        MarkDirty();
    }

    public void AddContent(string type)
    {
        var number = Content.Count(x => x.Type == type) + 1;
        var definition = new ContentDefinition(type, $"new_{type}_{number}", $"New {char.ToUpperInvariant(type[0])}{type[1..]} {number}");
        Content.Add(definition);
        SelectedContent = definition;
        Log.Add($"Created {type} definition '{definition.Id}'");
        MarkDirty();
    }

    public void ImportAsset(string path, string? name = null, AssetKind kind = AssetKind.Other)
    {
        var service = new AssetImportService();
        var record = service.Import(path, name, kind);
        Assets.Add(record);
        Log.Add($"Imported asset '{record.Name}' from {record.Path}");
        MarkDirty();
    }

    public void ReimportAsset(AssetRecord record)
    {
        var service = new AssetImportService();
        var updated = service.Reimport(record);
        var index = Assets.FindIndex(asset => asset.Id == record.Id);
        if (index >= 0)
        {
            Assets[index] = updated;
        }
        Log.Add($"Reimported asset '{updated.Name}'");
        MarkDirty();
    }

    public void BuildRuntimeBundle()
    {
        var service = new RuntimeBundleService();
        var result = service.CreateBundle(ProjectName, Assets, Content.Select(c => $"{c.Type}:{c.Id}"));
        Log.Add($"Built runtime bundle at {result.OutputDirectory}");
        RuntimeLaunchState = new RuntimeLaunchState("Ready", $"Runtime bundle written to {result.OutputDirectory}");
        Notify();
    }

    public void MarkDirty()
    {
        IsDirty = true;
        Changed?.Invoke();
    }

    public void Notify() => Changed?.Invoke();

    public void Save(string path)
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Environment.CurrentDirectory;
        var data = new ProjectFile
        {
            FormatVersion = ProjectFile.CurrentFormatVersion,
            ProjectName = ProjectName,
            TerrainWidth = Terrain.Width,
            TerrainHeight = Terrain.Height,
            TerrainHeights = Terrain.CopyHeights(),
            Objects = Objects.Select(o => new SceneObjectFile(o.Name, o.Category, o.Position.X, o.Position.Y, o.Position.Z, o.Rotation.X, o.Rotation.Y, o.Rotation.Z, o.Scale.X, o.Scale.Y, o.Scale.Z)).ToList(),
            Content = Content.Select(c => new ContentDefinitionFile(c.Type, c.Id, c.Name, c.Description, c.Level)).ToList(),
            WaterBodies = WaterBodies.Select(w => new WaterBodyFile(w.Id, w.Name, w.SurfaceElevation, w.Seeds.Select(c => new WorldCellFile(c.X, c.Z)).ToList(), new WaterSolveBoundsFile(w.Bounds.MinX, w.Bounds.MinZ, w.Bounds.MaxX, w.Bounds.MaxZ), w.MinimumDepth)).ToList(),
            Assets = Assets.Select(a => new AssetFile(a.Id, a.Name, MakeProjectRelativePath(projectDirectory, a.Path), a.Kind, a.IsNew)).ToList()
        };
        File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        IsDirty = false;
        Log.Add($"Saved project to {Path.GetFullPath(path)}");
        Changed?.Invoke();
    }

    public void Load(string path)
    {
        var data = JsonSerializer.Deserialize<ProjectFile>(File.ReadAllText(path)) ?? throw new InvalidDataException("Project file is empty.");
        if (data.FormatVersion > ProjectFile.CurrentFormatVersion)
            throw new InvalidDataException($"Project format {data.FormatVersion} is newer than this editor supports ({ProjectFile.CurrentFormatVersion}).");
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Environment.CurrentDirectory;
        ProjectName = data.ProjectName;
        var heights = data.TerrainHeights.Length > 0 ? data.TerrainHeights : Array.Empty<float>();
        Terrain = TerrainDocument.From(data.TerrainWidth, data.TerrainHeight, heights);
        Objects.Clear();
        foreach (var item in data.Objects)
        {
            Objects.Add(new SceneObject(item.Name, MeshFactory.Cube(.35f))
            {
                Category = item.Category,
                Position = new(item.X, item.Y, item.Z),
                Rotation = new(item.RotationX, item.RotationY, item.RotationZ),
                Scale = new(item.ScaleX, item.ScaleY, item.ScaleZ)
            });
        }
        Content.Clear();
        foreach (var item in data.Content)
            Content.Add(new ContentDefinition(item.Type, item.Id, item.Name) { Description = item.Description, Level = item.Level });
        ClearWaterPreview();
        WaterBodies.Clear();
        _solvedWaterBodies.Clear();
        foreach (var body in data.WaterBodies ?? [])
        {
            WaterBodies.Add(new WaterBodyDefinition(
                body.Id,
                body.Name,
                body.SurfaceElevation,
                body.Seeds.Select(s => new WorldCell(s.X, s.Z)).ToList(),
                new WaterSolveBounds(body.Bounds.MinX, body.Bounds.MinZ, body.Bounds.MaxX, body.Bounds.MaxZ),
                body.MinimumDepth));
        }
        Assets.Clear();
        foreach (var asset in data.Assets ?? [])
            Assets.Add(new AssetRecord(asset.Id, asset.Name, ResolveProjectPath(projectDirectory, asset.Path), asset.Kind, asset.IsNew));
        RebuildSolvedWaterBodies();
        History.Clear();
        Selected = Objects.FirstOrDefault();
        SelectedContent = Content.FirstOrDefault();
        ValidateProject();
        IsDirty = false;
        Log.Add($"Loaded project from {Path.GetFullPath(path)}");
        Changed?.Invoke();
    }

    private static string MakeProjectRelativePath(string projectDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        var full = Path.GetFullPath(path);
        return Path.GetRelativePath(projectDirectory, full).Replace('\\', '/');
    }

    private static string ResolveProjectPath(string projectDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(projectDirectory, path));
    }
}

public sealed class SceneObject(string name, MeshDocument mesh)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = name;
    public string Category { get; set; } = "World";
    public System.Numerics.Vector3 Position { get; set; }
    public System.Numerics.Vector3 Rotation { get; set; }
    public System.Numerics.Vector3 Scale { get; set; } = System.Numerics.Vector3.One;
    public MeshDocument Mesh { get; } = mesh;
    public override string ToString() => $"{Name}  [{Category}]";
}

public sealed class ContentDefinition(string type, string id, string name)
{
    public string Type { get; set; } = type;
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public override string ToString() => $"{Name}  ({Type}:{Id})";
}

public sealed class ProjectFile
{
    public const int CurrentFormatVersion = 2;
    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public string ProjectName { get; set; } = "Vaelor";
    public int TerrainWidth { get; set; } = 64;
    public int TerrainHeight { get; set; } = 64;
    public float[] TerrainHeights { get; set; } = [];
    public List<SceneObjectFile> Objects { get; set; } = [];
    public List<ContentDefinitionFile> Content { get; set; } = [];
    public List<WaterBodyFile> WaterBodies { get; set; } = [];
    public List<AssetFile> Assets { get; set; } = [];
}

public sealed record SceneObjectFile(string Name, string Category, float X, float Y, float Z, float RotationX = 0, float RotationY = 0, float RotationZ = 0, float ScaleX = 1, float ScaleY = 1, float ScaleZ = 1);
public sealed record ContentDefinitionFile(string Type, string Id, string Name, string Description, int Level);
public sealed record WaterBodyFile(Guid Id, string Name, float SurfaceElevation, List<WorldCellFile> Seeds, WaterSolveBoundsFile Bounds, float MinimumDepth = 0.02f);
public sealed record WorldCellFile(long X, long Z);
public sealed record WaterSolveBoundsFile(long MinX, long MinZ, long MaxX, long MaxZ);
public sealed record AssetFile(Guid Id, string Name, string Path, AssetKind Kind, bool IsNew = true);
