using FlaxEngine;

public static class SplineSampler
{
    public static float SampleOffset(Vector3 worldPos, SculptSplineSample[] splines, float maxHeight)
    {
        if (splines == null || splines.Length == 0)
            return 0f;

        float signed = 0f;
        foreach (var spline in splines)
            signed += spline.Sample(worldPos, maxHeight);

        return signed;
    }
}
