using FlaxEngine;

public static class BrushSampler
{
    public static float SampleOffset(Vector3 worldPos, SculptBrush[] brushes, float maxHeight, float globalSmoothing)
    {
        if (brushes == null || brushes.Length == 0)
            return 0f;

        float signed = 0f;
        foreach (var brush in brushes)
        {
            Vector3 localPos = brush.Actor.Transform.WorldToLocal(worldPos);
            float dist = brush.GetDistance(localPos);

            float inner = brush.Radius - brush.Falloff;
            float t = 1f - Mathf.Saturate((dist - inner) / Mathf.Max(brush.Falloff, 1f));
            if (t <= 0)
                continue;

            float smooth = t * t * (3f - 2f * t);
            float influence = Mathf.Lerp(t, smooth, globalSmoothing) * brush.Intensity;

            float heightDelta = influence * (brush.Actor.Position.Y / maxHeight);
            if (brush.Mode == SculptMode.Subtractive)
                heightDelta *= -1f;

            signed += heightDelta;
        }

        return signed;
    }
}
