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
    public TerrainBrushPreview BrushPreview { get; } = new();
    public TerrainDocument Terrain { get; private set; } = new(64, 64);
    public WaterBodyDefinition? ActiveWaterBody { get; private set; }
    public SolvedWaterBody? WaterPreview { get; private set; }
    public List<WaterBodyDefinition> WaterBodies { get; } = [];
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
        var solver = new WaterBodySolver(new GlobalElevationField
        {
            DefaultElevation = 0f
        });
        var terrain = Terrain;
        var field = new GlobalElevationField { DefaultElevation = terrain.CopyHeights().DefaultIfEmpty(0f).Average() };
        var heights = terrain.CopyHeights();
        for (int y = 0; y < terrain.Height; y++)
        for (int x = 0; x < terrain.Width; x++)
        {
            field.Set(new WorldVertex(x, y), heights[y * terrain.Width + x]);
        }

        var definition = new WaterBodyDefinition(
            Guid.NewGuid(),
            $"Preview Lake {seedX},{seedZ}",
            surfaceElevation,
            [new WorldCell(seedX, seedZ)],
            new WaterSolveBounds(Math.Max(0, seedX - 8), Math.Max(0, seedZ - 8), Math.Min(terrain.Width - 1, seedX + 8), Math.Min(terrain.Height - 1, seedZ + 8)));

        ActiveWaterBody = definition;
        WaterPreview = new WaterBodySolver(field).Solve(definition);
        WaterBodies.Add(definition);
        Log.Add($"Computed water-body preview for {definition.Name} using elevation {surfaceElevation:0.00}.");
        Notify();
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
        var data = new ProjectFile
        {
            ProjectName = ProjectName,
            TerrainWidth = Terrain.Width,
            TerrainHeight = Terrain.Height,
            TerrainHeights = Terrain.CopyHeights(),
            Objects = Objects.Select(o => new SceneObjectFile(o.Name, o.Category, o.Position.X, o.Position.Y, o.Position.Z, o.Rotation.X, o.Rotation.Y, o.Rotation.Z, o.Scale.X, o.Scale.Y, o.Scale.Z)).ToList(),
            Content = Content.Select(c => new ContentDefinitionFile(c.Type, c.Id, c.Name, c.Description, c.Level)).ToList(),
            WaterBodies = WaterBodies.Select(w => new WaterBodyFile(w.Id, w.Name, w.SurfaceElevation, w.Seeds.Select(c => new WorldCellFile(c.X, c.Z)).ToList(), new WaterSolveBoundsFile(w.Bounds.MinX, w.Bounds.MinZ, w.Bounds.MaxX, w.Bounds.MaxZ), w.MinimumDepth)).ToList(),
            Assets = Assets.Select(a => new AssetFile(a.Id, a.Name, a.Path, a.Kind, a.IsNew)).ToList()
        };
        File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        IsDirty = false;
        Log.Add($"Saved project to {Path.GetFullPath(path)}");
        Changed?.Invoke();
    }

    public void Load(string path)
    {
        var data = JsonSerializer.Deserialize<ProjectFile>(File.ReadAllText(path)) ?? throw new InvalidDataException("Project file is empty.");
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
        WaterBodies.Clear();
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
            Assets.Add(new AssetRecord(asset.Id, asset.Name, asset.Path, asset.Kind, asset.IsNew));
        Selected = Objects.FirstOrDefault();
        SelectedContent = Content.FirstOrDefault();
        ValidateProject();
        IsDirty = false;
        Log.Add($"Loaded project from {Path.GetFullPath(path)}");
        Changed?.Invoke();
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
