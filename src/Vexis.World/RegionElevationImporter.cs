namespace Vexis.World;

public enum BoundaryConflictMode
{
    PreserveWorldAndBlendImportedInterior,
    ReplaceWorldBoundary,
    RejectConflict
}

public sealed record RegionImportOptions(
    BoundaryConflictMode BoundaryMode = BoundaryConflictMode.PreserveWorldAndBlendImportedInterior,
    int TransitionWidth = 8,
    float ConflictTolerance = 0.01f);

/// <summary>
/// Imports externally authored region height data without crude edge averaging.
/// Existing global borders can be preserved while the imported interior is blended
/// over a configurable transition band, retaining cliffs when they are intentional.
/// </summary>
public sealed class RegionElevationImporter(GlobalElevationField field)
{
    public void Import(RegionCoordinate region, float[,] source, RegionImportOptions? options = null)
    {
        options ??= new RegionImportOptions();
        Validate(source, options);

        for (int z = 0; z < WorldGrid.VerticesPerRegion; z++)
        {
            for (int x = 0; x < WorldGrid.VerticesPerRegion; x++)
            {
                WorldVertex world = WorldGrid.ToWorldVertex(region, x, z);
                bool boundary = x is 0 or WorldGrid.CellsPerRegion || z is 0 or WorldGrid.CellsPerRegion;
                float incoming = source[x, z];
                float existing = field.Get(world);
                bool conflict = boundary && MathF.Abs(existing - incoming) > options.ConflictTolerance;

                if (conflict && options.BoundaryMode == BoundaryConflictMode.RejectConflict)
                {
                    throw new InvalidOperationException($"Terrain boundary conflict at world vertex ({world.X}, {world.Z}).");
                }

                if (conflict && options.BoundaryMode == BoundaryConflictMode.PreserveWorldAndBlendImportedInterior)
                {
                    continue;
                }

                field.Set(world, incoming);
            }
        }

        if (options.BoundaryMode == BoundaryConflictMode.PreserveWorldAndBlendImportedInterior)
        {
            BlendTransitionBand(region, source, options.TransitionWidth);
        }
    }

    private void BlendTransitionBand(RegionCoordinate region, float[,] source, int width)
    {
        if (width == 0)
        {
            return;
        }

        for (int z = 1; z < WorldGrid.CellsPerRegion; z++)
        {
            for (int x = 1; x < WorldGrid.CellsPerRegion; x++)
            {
                int distance = Math.Min(Math.Min(x, z), Math.Min(WorldGrid.CellsPerRegion - x, WorldGrid.CellsPerRegion - z));
                if (distance > width)
                {
                    continue;
                }

                float t = SmoothStep(distance / (float)width);
                WorldVertex world = WorldGrid.ToWorldVertex(region, x, z);
                float boundaryGuided = EstimateNearestBoundary(region, x, z);
                field.Set(world, Lerp(boundaryGuided, source[x, z], t));
            }
        }
    }

    private float EstimateNearestBoundary(RegionCoordinate region, int localX, int localZ)
    {
        int left = localX;
        int right = WorldGrid.CellsPerRegion - localX;
        int top = localZ;
        int bottom = WorldGrid.CellsPerRegion - localZ;
        int min = Math.Min(Math.Min(left, right), Math.Min(top, bottom));

        return min switch
        {
            _ when min == left => field.Get(WorldGrid.ToWorldVertex(region, 0, localZ)),
            _ when min == right => field.Get(WorldGrid.ToWorldVertex(region, WorldGrid.CellsPerRegion, localZ)),
            _ when min == top => field.Get(WorldGrid.ToWorldVertex(region, localX, 0)),
            _ => field.Get(WorldGrid.ToWorldVertex(region, localX, WorldGrid.CellsPerRegion))
        };
    }

    private static float SmoothStep(float value)
    {
        float t = Math.Clamp(value, 0f, 1f);
        return t * t * (3f - (2f * t));
    }

    private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

    private static void Validate(float[,] source, RegionImportOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.GetLength(0) != WorldGrid.VerticesPerRegion || source.GetLength(1) != WorldGrid.VerticesPerRegion)
        {
            throw new ArgumentException("Imported terrain must contain exactly 65x65 vertices.", nameof(source));
        }

        if (options.TransitionWidth is < 0 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Transition width must be between 0 and 32 vertices.");
        }
    }
}
