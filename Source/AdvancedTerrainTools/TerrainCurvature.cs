using FlaxEngine;

public static class TerrainCurvature
{
    public static void Apply(float[] map, int size, float ridgeGain, float valleyRelax, int iterations)
    {
        if (map == null || map.Length != size * size || iterations <= 0)
            return;

        ridgeGain = Mathf.Max(0f, ridgeGain);
        valleyRelax = Mathf.Max(0f, valleyRelax);

        float[] buffer = new float[map.Length];
        float[] src = map;
        float[] dst = buffer;

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int y = 0; y < size; y++)
            {
                int yOffset = y * size;
                for (int x = 0; x < size; x++)
                {
                    int idx = yOffset + x;

                    if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                    {
                        dst[idx] = src[idx];
                        continue;
                    }

                    float h = src[idx];
                    float avg = (src[idx - 1] + src[idx + 1] + src[idx - size] + src[idx + size]) * 0.25f;
                    float diff = h - avg;

                    float delta = diff > 0f ? diff * ridgeGain : diff * valleyRelax;

                    delta = Mathf.Clamp(delta, -0.05f, 0.05f);

                    dst[idx] = h + delta;
                }
            }

            var tmp = src;
            src = dst;
            dst = tmp;
        }

        if (!ReferenceEquals(src, map))
            src.CopyTo(map, 0);
    }
}