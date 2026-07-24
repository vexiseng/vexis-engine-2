namespace Vexis.World;

public readonly record struct WorldVertex(long X, long Z);
public readonly record struct WorldCell(long X, long Z);
public readonly record struct RegionCoordinate(int X, int Z);

public static class WorldGrid
{
    public const int CellsPerRegion = 64;
    public const int VerticesPerRegion = CellsPerRegion + 1;

    public static RegionCoordinate RegionContaining(WorldCell cell) => new(
        FloorDiv(cell.X, CellsPerRegion),
        FloorDiv(cell.Z, CellsPerRegion));

    public static WorldCell RegionOrigin(RegionCoordinate region) => new(
        (long)region.X * CellsPerRegion,
        (long)region.Z * CellsPerRegion);

    public static WorldVertex ToWorldVertex(RegionCoordinate region, int localX, int localZ)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(localX);
        ArgumentOutOfRangeException.ThrowIfNegative(localZ);
        if (localX >= VerticesPerRegion || localZ >= VerticesPerRegion)
        {
            throw new ArgumentOutOfRangeException(nameof(localX), "Local terrain vertices must be in the inclusive range 0..64.");
        }

        WorldCell origin = RegionOrigin(region);
        return new WorldVertex(origin.X + localX, origin.Z + localZ);
    }

    private static int FloorDiv(long value, int divisor)
    {
        long quotient = value / divisor;
        long remainder = value % divisor;
        if (remainder < 0)
        {
            quotient--;
        }

        return checked((int)quotient);
    }
}
