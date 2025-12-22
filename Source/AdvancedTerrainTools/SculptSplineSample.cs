using FlaxEngine;

public struct SculptSplineSample
{
    public SculptMode Mode;
    public float Intensity;
    public float Width;
    public float Falloff;
    public float Smoothing;
    public Vector3[] Points;

    public float Sample(Vector3 worldPos, float maxHeight)
    {
        if (Points == null || Points.Length < 2)
            return 0f;

        float minDist = float.MaxValue;
        Vector3 nearest = Vector3.Zero;
        Vector2 worldXZ = new Vector2(worldPos.X, worldPos.Z);

        for (int i = 0; i < Points.Length - 1; i++)
        {
            Vector3 a = Points[i];
            Vector3 b = Points[i + 1];

            Vector2 aXZ = new Vector2(a.X, a.Z);
            Vector2 bXZ = new Vector2(b.X, b.Z);
            Vector2 dir = bXZ - aXZ;
            float lenSq = dir.LengthSquared;
            float segT = lenSq > 1e-6f ? Mathf.Saturate(Vector2.Dot(worldXZ - aXZ, dir) / lenSq) : 0f;

            Vector2 pXZ = aXZ + dir * segT;
            float dist = (worldXZ - pXZ).Length;
            if (dist < minDist)
            {
                minDist = dist;
                nearest = Vector3.Lerp(a, b, segT);
            }
        }

        float inner = Width - Falloff;
        float t = 1f - Mathf.Saturate((minDist - inner) / Mathf.Max(Falloff, 1f));
        if (t <= 0f)
            return 0f;

        float smooth = t * t * (3f - 2f * t);
        float influence = Mathf.Lerp(t, smooth, Smoothing) * Intensity;

        float heightDelta = influence * (nearest.Y / maxHeight);
        if (Mode == SculptMode.Subtractive)
            heightDelta *= -1f;

        return heightDelta;
    }
}
