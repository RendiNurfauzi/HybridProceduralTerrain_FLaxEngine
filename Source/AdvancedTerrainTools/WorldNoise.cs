using FlaxEngine;
using FlaxEngine.Utilities;

public static class WorldNoise
{
    public static float GetHeight(
        float x,
        float z,
        float scale,
        float roughness,
        bool useMask,
        float maskScale,
        bool useTerrace,
        float steps,
        float ridge,
        float brushOffset,
        bool useDirectionalWarp,
        float warpStrength,
        float warpAngleDeg,
        float warpFrequency)
    {
        float warpBase = 15f;

        float wx = x + Noise.PerlinNoise(new Float2(x * 0.002f, z * 0.002f)) * warpBase;
        float wz = z + Noise.PerlinNoise(new Float2(z * 0.002f, x * 0.002f)) * warpBase;

        if (useDirectionalWarp && warpStrength > 0f)
        {
            float angleRad = warpAngleDeg * Mathf.DegreesToRadians;
            Float2 dir = new Float2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            Float2 orth = new Float2(-dir.Y, dir.X);

            float freq = Mathf.Max(0.0001f, warpFrequency);
            float a = Noise.PerlinNoise(new Float2(x * freq, z * freq));
            float b = Noise.PerlinNoise(new Float2((x + 123.45f) * freq, (z - 987.65f) * freq));

            wx += (orth.X * a + dir.X * b * 0.5f) * warpStrength;
            wz += (orth.Y * a + dir.Y * b * 0.5f) * warpStrength;
        }

        float v = Noise.PerlinNoise(new Float2(wx * scale, wz * scale));
        v = 1f - Mathf.Abs(v * 2f - 1f);
        v = Mathf.Pow(v, ridge);

        float detail = Noise.PerlinNoise(new Float2(wx * scale * 3.5f, wz * scale * 3.5f));
        v += detail * roughness;

        if (useMask)
        {
            float m = Noise.PerlinNoise(new Float2(wx * scale * maskScale, wz * scale * maskScale));
            m = Mathf.Saturate(m * 1.5f - 0.2f);
            v *= m;
        }

        float height = v * 0.3f;

        height += brushOffset;

        if (useTerrace)
            height = Mathf.Round(height * steps) / steps;

        return height;
    }
}