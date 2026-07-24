using System.Collections.ObjectModel;
using System.Text.Json;
using Vexis.Modeling;
using Vexis.Runtime;

namespace Vexis.Editor.Desktop;

public sealed class EditorState
{
    public ObservableCollection<SceneObject> Objects { get; } = [];
    public ObservableCollection<string> Log { get; } = [];
    public ObservableCollection<ContentDefinition> Content { get; } = [];
    public RuntimeSession Runtime { get; } = new();
    public TerrainBrushPreview BrushPreview { get; } = new();
    public TerrainDocument Terrain { get; private set; } = new(64, 64);
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
            Content = Content.Select(c => new ContentDefinitionFile(c.Type, c.Id, c.Name, c.Description, c.Level)).ToList()
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
        Terrain = TerrainDocument.From(data.TerrainWidth, data.TerrainHeight, data.TerrainHeights);
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
        Selected = Objects.FirstOrDefault();
        SelectedContent = Content.FirstOrDefault();
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
}

public sealed record SceneObjectFile(string Name, string Category, float X, float Y, float Z, float RotationX = 0, float RotationY = 0, float RotationZ = 0, float ScaleX = 1, float ScaleY = 1, float ScaleZ = 1);
public sealed record ContentDefinitionFile(string Type, string Id, string Name, string Description, int Level);
