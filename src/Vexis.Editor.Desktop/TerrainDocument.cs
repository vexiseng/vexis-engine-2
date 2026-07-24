namespace Vexis.Editor.Desktop;

public sealed class TerrainDocument
{
    private readonly float[] _heights;
    public int Width { get; }
    public int Height { get; }
    public int Revision { get; private set; }

    public TerrainDocument(int width, int height)
    {
        if (width < 2 || height < 2) throw new ArgumentOutOfRangeException(nameof(width));
        Width = width;
        Height = height;
        _heights = new float[width * height];
        GenerateStarterTerrain();
    }

    public float this[int x, int y]
    {
        get => _heights[y * Width + x];
        set => _heights[y * Width + x] = value;
    }

    public void Sculpt(int centerX, int centerY, float radius, float strength, TerrainBrushMode mode)
    {
        var minX = Math.Max(0, (int)Math.Floor(centerX - radius));
        var maxX = Math.Min(Width - 1, (int)Math.Ceiling(centerX + radius));
        var minY = Math.Max(0, (int)Math.Floor(centerY - radius));
        var maxY = Math.Min(Height - 1, (int)Math.Ceiling(centerY + radius));
        var centerHeight = this[Math.Clamp(centerX, 0, Width - 1), Math.Clamp(centerY, 0, Height - 1)];

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var distance = MathF.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
            if (distance > radius) continue;
            var falloff = 1f - distance / Math.Max(radius, .001f);
            switch (mode)
            {
                case TerrainBrushMode.Raise: this[x, y] += strength * falloff; break;
                case TerrainBrushMode.Lower: this[x, y] -= strength * falloff; break;
                case TerrainBrushMode.Flatten: this[x, y] += (centerHeight - this[x, y]) * Math.Clamp(strength * falloff, 0, 1); break;
                case TerrainBrushMode.Smooth:
                    var average = NeighborhoodAverage(x, y);
                    this[x, y] += (average - this[x, y]) * Math.Clamp(strength * falloff, 0, 1);
                    break;
            }
        }
        Revision++;
    }

    public void FlattenAll(float height = 0)
    {
        Array.Fill(_heights, height);
        Revision++;
    }

    public void GenerateRollingTerrain(int seed, float amplitude = 2.5f)
    {
        var random = new Random(seed);
        var phaseX = (float)random.NextDouble() * MathF.PI * 2;
        var phaseY = (float)random.NextDouble() * MathF.PI * 2;
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var nx = x / (float)Math.Max(1, Width - 1);
            var ny = y / (float)Math.Max(1, Height - 1);
            var broad = MathF.Sin(nx * MathF.PI * 3 + phaseX) + MathF.Cos(ny * MathF.PI * 2 + phaseY);
            var detail = MathF.Sin((nx + ny) * MathF.PI * 9 + phaseX * .5f) * .25f;
            this[x, y] = (broad * .5f + detail) * amplitude;
        }
        Revision++;
    }

    public float[] CopyHeights() => (float[])_heights.Clone();

    public static TerrainDocument From(int width, int height, float[] heights)
    {
        var terrain = new TerrainDocument(width, height);
        if (heights.Length != width * height) throw new InvalidDataException("Terrain dimensions do not match height data.");
        Array.Copy(heights, terrain._heights, heights.Length);
        terrain.Revision++;
        return terrain;
    }

    private float NeighborhoodAverage(int x, int y)
    {
        float total = 0;
        var count = 0;
        for (var oy = -1; oy <= 1; oy++)
        for (var ox = -1; ox <= 1; ox++)
        {
            var nx = x + ox;
            var ny = y + oy;
            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
            total += this[nx, ny];
            count++;
        }
        return total / Math.Max(1, count);
    }

    private void GenerateStarterTerrain()
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var nx = x / (float)(Width - 1);
            var ny = y / (float)(Height - 1);
            this[x, y] = MathF.Sin(nx * MathF.PI * 3) * .8f + MathF.Cos(ny * MathF.PI * 2) * .55f;
        }
    }
}

public enum TerrainBrushMode { Raise, Lower, Smooth, Flatten }
