namespace Vexis.World;

/// <summary>
/// Canonical terrain elevation store. Adjacent regions reference the same world-space
/// border vertices, so a seam cannot exist in persisted terrain data.
/// </summary>
public sealed class GlobalElevationField
{
    private readonly Dictionary<WorldVertex, float> _elevations = [];

    public float DefaultElevation { get; init; }

    public int Count => _elevations.Count;

    public float Get(WorldVertex vertex) =>
        _elevations.TryGetValue(vertex, out float value) ? value : DefaultElevation;

    public void Set(WorldVertex vertex, float elevation)
    {
        if (!float.IsFinite(elevation))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite.");
        }

        _elevations[vertex] = elevation;
    }

    public float[,] ReadRegion(RegionCoordinate region)
    {
        var result = new float[WorldGrid.VerticesPerRegion, WorldGrid.VerticesPerRegion];
        for (int z = 0; z < WorldGrid.VerticesPerRegion; z++)
        {
            for (int x = 0; x < WorldGrid.VerticesPerRegion; x++)
            {
                result[x, z] = Get(WorldGrid.ToWorldVertex(region, x, z));
            }
        }

        return result;
    }

    public void WriteRegionInterior(RegionCoordinate region, float[,] elevations)
    {
        ValidateRegionArray(elevations);

        // Borders are included, but because they map to global keys, both neighboring
        // region views immediately see the same value. There are no duplicate edges.
        for (int z = 0; z < WorldGrid.VerticesPerRegion; z++)
        {
            for (int x = 0; x < WorldGrid.VerticesPerRegion; x++)
            {
                Set(WorldGrid.ToWorldVertex(region, x, z), elevations[x, z]);
            }
        }
    }

    public float GetCellAverage(WorldCell cell)
    {
        float a = Get(new WorldVertex(cell.X, cell.Z));
        float b = Get(new WorldVertex(cell.X + 1, cell.Z));
        float c = Get(new WorldVertex(cell.X, cell.Z + 1));
        float d = Get(new WorldVertex(cell.X + 1, cell.Z + 1));
        return (a + b + c + d) * 0.25f;
    }

    private static void ValidateRegionArray(float[,] elevations)
    {
        ArgumentNullException.ThrowIfNull(elevations);
        if (elevations.GetLength(0) != WorldGrid.VerticesPerRegion ||
            elevations.GetLength(1) != WorldGrid.VerticesPerRegion)
        {
            throw new ArgumentException("A region elevation array must be exactly 65x65 vertices.", nameof(elevations));
        }
    }
}
