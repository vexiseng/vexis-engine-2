namespace Vexis.World;

public sealed record WaterSolveBounds(long MinX, long MinZ, long MaxX, long MaxZ)
{
    public bool Contains(WorldCell cell) =>
        cell.X >= MinX && cell.X <= MaxX && cell.Z >= MinZ && cell.Z <= MaxZ;
}

public sealed record WaterBodyDefinition(
    Guid Id,
    string Name,
    float SurfaceElevation,
    IReadOnlyList<WorldCell> Seeds,
    WaterSolveBounds Bounds,
    float MinimumDepth = 0.02f);

public sealed record WaterCell(
    WorldCell Coordinate,
    float SurfaceElevation,
    float TerrainElevation,
    float Depth,
    int ShoreDistance);

public sealed class SolvedWaterBody
{
    public required WaterBodyDefinition Definition { get; init; }
    public required IReadOnlyDictionary<WorldCell, WaterCell> Cells { get; init; }
}

/// <summary>
/// Deterministic global flood solver. A water body is defined by semantic seeds,
/// one surface elevation, and explicit bounds. The solver decides coverage from
/// terrain elevation and connectivity, not from opaque per-region paint state.
/// </summary>
public sealed class WaterBodySolver(GlobalElevationField terrain)
{
    private static readonly WorldCell[] NeighborOffsets =
    [
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    ];

    public SolvedWaterBody Solve(WaterBodyDefinition definition)
    {
        Validate(definition);

        var wet = new HashSet<WorldCell>();
        var queue = new Queue<WorldCell>();

        foreach (WorldCell seed in definition.Seeds)
        {
            if (CanFlood(seed, definition))
            {
                queue.Enqueue(seed);
                wet.Add(seed);
            }
        }

        while (queue.Count > 0)
        {
            WorldCell current = queue.Dequeue();
            foreach (WorldCell offset in NeighborOffsets)
            {
                var next = new WorldCell(current.X + offset.X, current.Z + offset.Z);
                if (!wet.Contains(next) && CanFlood(next, definition))
                {
                    wet.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        Dictionary<WorldCell, int> shoreDistances = ComputeShoreDistances(wet);
        var cells = new Dictionary<WorldCell, WaterCell>(wet.Count);
        foreach (WorldCell cell in wet)
        {
            float ground = terrain.GetCellAverage(cell);
            cells[cell] = new WaterCell(
                cell,
                definition.SurfaceElevation,
                ground,
                MathF.Max(0f, definition.SurfaceElevation - ground),
                shoreDistances[cell]);
        }

        return new SolvedWaterBody { Definition = definition, Cells = cells };
    }

    private bool CanFlood(WorldCell cell, WaterBodyDefinition definition)
    {
        if (!definition.Bounds.Contains(cell))
        {
            return false;
        }

        float depth = definition.SurfaceElevation - terrain.GetCellAverage(cell);
        return depth >= definition.MinimumDepth;
    }

    private static Dictionary<WorldCell, int> ComputeShoreDistances(HashSet<WorldCell> wet)
    {
        var distances = new Dictionary<WorldCell, int>(wet.Count);
        var queue = new Queue<WorldCell>();

        foreach (WorldCell cell in wet)
        {
            bool shore = NeighborOffsets.Any(offset => !wet.Contains(new WorldCell(cell.X + offset.X, cell.Z + offset.Z)));
            if (shore)
            {
                distances[cell] = 0;
                queue.Enqueue(cell);
            }
        }

        while (queue.Count > 0)
        {
            WorldCell current = queue.Dequeue();
            int nextDistance = distances[current] + 1;
            foreach (WorldCell offset in NeighborOffsets)
            {
                var next = new WorldCell(current.X + offset.X, current.Z + offset.Z);
                if (wet.Contains(next) && !distances.ContainsKey(next))
                {
                    distances[next] = nextDistance;
                    queue.Enqueue(next);
                }
            }
        }

        return distances;
    }

    private static void Validate(WaterBodyDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Seeds.Count == 0)
        {
            throw new ArgumentException("A water body requires at least one seed cell.", nameof(definition));
        }

        if (!float.IsFinite(definition.SurfaceElevation))
        {
            throw new ArgumentOutOfRangeException(nameof(definition), "Water surface elevation must be finite.");
        }

        if (definition.Bounds.MaxX < definition.Bounds.MinX || definition.Bounds.MaxZ < definition.Bounds.MinZ)
        {
            throw new ArgumentException("Water solve bounds are invalid.", nameof(definition));
        }
    }
}
