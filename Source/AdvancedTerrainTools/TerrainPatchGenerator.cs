using System.Collections.Generic;
using FlaxEngine;

public static class TerrainPatchGenerator
{
    public static List<PatchHeightmap> Generate(
        Vector3 terrainOrigin,
        int size,
        Int2 patchGrid,
        float sampleSpacing,
        SculptBrush[] brushes,
        SculptSplineSample[] splines,
        HybridProceduralTerrain settings)
    {
        int patchCountX = Mathf.Max(1, patchGrid.X);
        int patchCountY = Mathf.Max(1, patchGrid.Y);
        float patchWorldSize = (size - 1) * sampleSpacing;

        var patchMaps = new List<PatchHeightmap>(patchCountX * patchCountY);

        for (int py = 0; py < patchCountY; py++)
        {
            for (int px = 0; px < patchCountX; px++)
            {
                Int2 coord = new Int2(px, py);
                float[] heightMap = new float[size * size];
                Vector3 patchOrigin = terrainOrigin + new Vector3(px * patchWorldSize, 0f, py * patchWorldSize);

                FillPatchHeightMap(size, patchOrigin, sampleSpacing, brushes, splines, heightMap, settings);

                if (settings.UseCurvatureDetail)
                    TerrainCurvature.Apply(heightMap, size, settings.RidgeGain, settings.ValleyRelax, settings.CurvatureIterations);

                if (settings.Droplets > 0)
                    WorldHydrology.Erode(heightMap, size, settings.Droplets, settings.ErosionRate, settings.DepositionRate, settings.Gravity);

                if (settings.ThermalStrength > 0)
                    WorldHydrology.ApplyThermal(heightMap, size, settings.ThermalStrength, settings.AngleThreshold);

                if (settings.BeachEnabled && settings.BeachWidth > 0f)
                    BeachFalloff.Apply(heightMap, size, sampleSpacing, settings.BeachWidth, settings.BeachSmoothness);

                for (int i = 0; i < heightMap.Length; i++)
                    heightMap[i] = Mathf.Saturate(heightMap[i]) * settings.MaxHeight;

                patchMaps.Add(new PatchHeightmap { Coord = coord, Map = heightMap });
            }
        }

        return patchMaps;
    }

    private static void FillPatchHeightMap(int size, Vector3 patchOrigin, float sampleSpacing, SculptBrush[] brushes, SculptSplineSample[] splines, float[] heightMap, HybridProceduralTerrain settings)
    {
        for (int z = 0; z < size; z++)
        {
            int zOffset = z * size;
            for (int x = 0; x < size; x++)
            {
                Vector3 worldPos = patchOrigin + new Vector3(x * sampleSpacing, 0, z * sampleSpacing);

                float signedBrushOffset = BrushSampler.SampleOffset(worldPos, brushes, settings.MaxHeight, settings.GlobalSmoothing);
                float splineOffset = SplineSampler.SampleOffset(worldPos, splines, settings.MaxHeight);
                float signedOffset = signedBrushOffset + splineOffset;

                float h = WorldNoise.GetHeight(
                    x, z,
                    settings.NoiseScale,
                    settings.Roughness,
                    settings.UseMountainMask,
                    settings.MountainMaskScale,
                    settings.UseTerrace,
                    settings.TerraceSteps,
                    settings.RidgeSharpness,
                    signedOffset,
                    settings.UseDirectionalWarp,
                    settings.WarpStrength,
                    settings.WarpAngleDeg,
                    settings.WarpFrequency
                );

                heightMap[zOffset + x] = h;
            }
        }
    }
}
