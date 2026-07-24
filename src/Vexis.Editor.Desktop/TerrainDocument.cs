namespace Vexis.Editor.Desktop;

public sealed class TerrainDocument
{
    private readonly float[] _heights;
    private readonly Random _brushRandom = new();

    public int Width { get; }
    public int Height { get; }
    public int Revision { get; private set; }

    public TerrainDocument(int width, int height)
    {
        if (width < 2 || height < 2)
            throw new ArgumentOutOfRangeException(nameof(width));

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

    public void Sculpt(
        int centerX,
        int centerY,
        float radius,
        float strength,
        TerrainBrushMode mode,
        float falloff = .55f,
        float targetHeight = 0f,
        int? seed = null)
    {
        radius = Math.Max(.25f, radius);
        strength = Math.Clamp(strength, .001f, 2f);
        falloff = Math.Clamp(falloff, 0f, .95f);

        if (mode == TerrainBrushMode.Ramp)
        {
            ApplyRamp(centerX - (int)radius, centerY, centerX + (int)radius, centerY, radius * .45f, strength);
            return;
        }

        var minX = Math.Max(0, (int)Math.Floor(centerX - radius));
        var maxX = Math.Min(Width - 1, (int)Math.Ceiling(centerX + radius));
        var minY = Math.Max(0, (int)Math.Floor(centerY - radius));
        var maxY = Math.Min(Height - 1, (int)Math.Ceiling(centerY + radius));
        var source = mode is TerrainBrushMode.Smooth or TerrainBrushMode.Erode
            ? CopyHeights()
            : _heights;

        var sampledCenter = this[Math.Clamp(centerX, 0, Width - 1), Math.Clamp(centerY, 0, Height - 1)];
        var flattenHeight = mode == TerrainBrushMode.SetHeight ? targetHeight : sampledCenter;
        var noiseSeed = seed ?? _brushRandom.Next();

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var dx = x - centerX;
            var dy = y - centerY;
            var distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance > radius)
                continue;

            var normalized = distance / radius;
            var weight = FalloffWeight(normalized, falloff);
            if (weight <= .0001f)
                continue;

            switch (mode)
            {
                case TerrainBrushMode.Raise:
                    this[x, y] += strength * weight;
                    break;

                case TerrainBrushMode.Lower:
                    this[x, y] -= strength * weight;
                    break;

                case TerrainBrushMode.Flatten:
                case TerrainBrushMode.SetHeight:
                {
                    var blend = Math.Clamp(strength * weight, 0f, 1f);
                    this[x, y] += (flattenHeight - this[x, y]) * blend;
                    break;
                }

                case TerrainBrushMode.Smooth:
                {
                    var average = NeighborhoodAverage(source, x, y, 1);
                    var blend = Math.Clamp(strength * weight, 0f, 1f);
                    this[x, y] = source[y * Width + x] + (average - source[y * Width + x]) * blend;
                    break;
                }

                case TerrainBrushMode.Noise:
                {
                    var broad = ValueNoise(x, y, noiseSeed, .11f);
                    var detail = ValueNoise(x, y, noiseSeed + 7919, .31f) * .35f;
                    this[x, y] += (broad + detail) * strength * weight;
                    break;
                }

                case TerrainBrushMode.Erode:
                {
                    var current = source[y * Width + x];
                    var average = NeighborhoodAverage(source, x, y, 2);
                    var steepest = SteepestNeighbor(source, x, y);
                    var talus = MathF.Abs(current - steepest);
                    var erosion = talus > .18f
                        ? (steepest - current) * .42f
                        : (average - current) * .16f;
                    this[x, y] = current + erosion * Math.Clamp(strength * weight, 0f, 1f);
                    break;
                }
            }
        }

        ClampHeights(-40f, 120f);
        Revision++;
    }

    public void ApplyRamp(int startX, int startY, int endX, int endY, float width, float strength)
    {
        var startHeight = this[Math.Clamp(startX, 0, Width - 1), Math.Clamp(startY, 0, Height - 1)];
        var endHeight = this[Math.Clamp(endX, 0, Width - 1), Math.Clamp(endY, 0, Height - 1)];
        var vx = endX - startX;
        var vy = endY - startY;
        var lengthSquared = Math.Max(1f, vx * vx + vy * vy);

        var minX = Math.Max(0, (int)MathF.Floor(Math.Min(startX, endX) - width));
        var maxX = Math.Min(Width - 1, (int)MathF.Ceiling(Math.Max(startX, endX) + width));
        var minY = Math.Max(0, (int)MathF.Floor(Math.Min(startY, endY) - width));
        var maxY = Math.Min(Height - 1, (int)MathF.Ceiling(Math.Max(startY, endY) + width));

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var wx = x - startX;
            var wy = y - startY;
            var t = Math.Clamp((wx * vx + wy * vy) / lengthSquared, 0f, 1f);
            var closestX = startX + vx * t;
            var closestY = startY + vy * t;
            var distance = MathF.Sqrt((x - closestX) * (x - closestX) + (y - closestY) * (y - closestY));
            if (distance > width)
                continue;

            var edgeWeight = 1f - distance / Math.Max(.001f, width);
            var target = startHeight + (endHeight - startHeight) * t;
            var blend = Math.Clamp(strength * edgeWeight, 0f, 1f);
            this[x, y] += (target - this[x, y]) * blend;
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
        var phaseX = (float)random.NextDouble() * MathF.Tau;
        var phaseY = (float)random.NextDouble() * MathF.Tau;

        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var nx = x / (float)Math.Max(1, Width - 1);
            var ny = y / (float)Math.Max(1, Height - 1);
            var broad = MathF.Sin(nx * MathF.PI * 3 + phaseX) + MathF.Cos(ny * MathF.PI * 2 + phaseY);
            var ridges = 1f - MathF.Abs(MathF.Sin((nx * .8f + ny) * MathF.PI * 5 + phaseX));
            var detail = ValueNoise(x, y, seed + 31337, .14f) * .35f;
            this[x, y] = (broad * .42f + ridges * .34f + detail) * amplitude;
        }

        Revision++;
    }

    public float[] CopyHeights() => (float[])_heights.Clone();

    public void ResetToHeight(float height)
    {
        Array.Fill(_heights, height);
        Revision++;
    }

    public void Smoothen(int radius = 1)
    {
        var source = CopyHeights();
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var average = NeighborhoodAverage(source, x, y, radius);
            this[x, y] = average;
        }
        Revision++;
    }

    public static TerrainDocument From(int width, int height, float[] heights)
    {
        var terrain = new TerrainDocument(width, height);
        if (heights.Length != width * height)
            throw new InvalidDataException("Terrain dimensions do not match height data.");

        Array.Copy(heights, terrain._heights, heights.Length);
        terrain.Revision++;
        return terrain;
    }

    private float NeighborhoodAverage(float[] source, int x, int y, int radius)
    {
        float total = 0;
        var count = 0;

        for (var oy = -radius; oy <= radius; oy++)
        for (var ox = -radius; ox <= radius; ox++)
        {
            var nx = x + ox;
            var ny = y + oy;
            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
                continue;

            total += source[ny * Width + nx];
            count++;
        }

        return total / Math.Max(1, count);
    }

    private float SteepestNeighbor(float[] source, int x, int y)
    {
        var current = source[y * Width + x];
        var selected = current;
        var selectedDelta = 0f;

        for (var oy = -1; oy <= 1; oy++)
        for (var ox = -1; ox <= 1; ox++)
        {
            if (ox == 0 && oy == 0)
                continue;

            var nx = x + ox;
            var ny = y + oy;
            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
                continue;

            var value = source[ny * Width + nx];
            var delta = MathF.Abs(value - current);
            if (delta > selectedDelta)
            {
                selectedDelta = delta;
                selected = value;
            }
        }

        return selected;
    }

    private static float FalloffWeight(float normalizedDistance, float hardness)
    {
        normalizedDistance = Math.Clamp(normalizedDistance, 0f, 1f);
        var softStart = Math.Clamp(hardness, 0f, .95f);
        if (normalizedDistance <= softStart)
            return 1f;

        var t = (normalizedDistance - softStart) / Math.Max(.001f, 1f - softStart);
        return 1f - t * t * (3f - 2f * t);
    }

    private static float ValueNoise(int x, int y, int seed, float scale)
    {
        var fx = x * scale;
        var fy = y * scale;
        var x0 = (int)MathF.Floor(fx);
        var y0 = (int)MathF.Floor(fy);
        var tx = fx - x0;
        var ty = fy - y0;

        var sx = tx * tx * (3f - 2f * tx);
        var sy = ty * ty * (3f - 2f * ty);
        var a = Lerp(HashSigned(x0, y0, seed), HashSigned(x0 + 1, y0, seed), sx);
        var b = Lerp(HashSigned(x0, y0 + 1, seed), HashSigned(x0 + 1, y0 + 1, seed), sx);
        return Lerp(a, b, sy);
    }

    private static float HashSigned(int x, int y, int seed)
    {
        unchecked
        {
            var h = seed;
            h = h * 31 + x;
            h = h * 31 + y;
            h ^= h << 13;
            h ^= h >> 17;
            h ^= h << 5;
            return ((h & 0x7fffffff) / 1073741823.5f) - 1f;
        }
    }

    private void ClampHeights(float min, float max)
    {
        for (var i = 0; i < _heights.Length; i++)
            _heights[i] = Math.Clamp(_heights[i], min, max);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

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

public enum TerrainBrushMode
{
    Raise,
    Lower,
    Smooth,
    Flatten,
    SetHeight,
    Noise,
    Ramp,
    Erode
}
