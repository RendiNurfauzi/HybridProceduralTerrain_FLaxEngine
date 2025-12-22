using FlaxEngine;

public static class BeachFalloff
{
    public static void Apply(float[] heightMap, int size, float sampleSpacing, float beachWidth, float beachSmoothness)
    {
        float invBeach = 1f / beachWidth;
        float exp = Mathf.Max(0.1f, beachSmoothness);

        for (int z = 0; z < size; z++)
        {
            int zIdx = z * size;
            int distZ = Mathf.Min(z, size - 1 - z);
            for (int x = 0; x < size; x++)
            {
                int distX = Mathf.Min(x, size - 1 - x);
                float edgeDistMeters = Mathf.Min(distX, distZ) * sampleSpacing;
                float t = Mathf.Pow(Mathf.Saturate(edgeDistMeters * invBeach), exp);
                heightMap[zIdx + x] *= t;
            }
        }
    }
}
