using FlaxEngine;

public class SculptBrush : Script
{
    [Header("Brush Settings")]
    public BrushShape Shape = BrushShape.Sphere;
    public SculptMode Mode = SculptMode.Additive;

    [Range(0, 2f)]
    public float Intensity = 1f;

    public float Radius = 1500f;
    public float Falloff = 700f;

    [Header("Artistic Variation")]
    [Range(0, 500)]
    public float EdgeNoise = 0f;
    public float GetDistance(Vector3 localPos)
    {
        switch (Shape)
        {
            case BrushShape.Sphere:
                return localPos.Length;

            case BrushShape.Box:
                float dx = Mathf.Max(Mathf.Abs(localPos.X) - Radius, 0);
                float dy = Mathf.Max(Mathf.Abs(localPos.Y) - Radius, 0);
                float dz = Mathf.Max(Mathf.Abs(localPos.Z) - Radius, 0);
                return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

            case BrushShape.Capsule:
                Vector3 a = new Vector3(0, 0, Radius);
                Vector3 b = new Vector3(0, 0, -Radius);
                Vector3 pa = localPos - a;
                Vector3 ba = b - a;
                float h = Mathf.Saturate(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba));
                return (pa - ba * h).Length;
        }

        return float.MaxValue;
    }
    public override void OnDebugDraw()
    {
        Color mainColor = Mode == SculptMode.Additive ? Color.Orange : Color.Cyan;
        Color falloffColor = mainColor * 0.5f;

        Transform t = Actor.Transform;
        Vector3 pos = t.Translation;
        Vector3 scale = t.Scale;

        float maxScale = Mathf.Max(scale.X, Mathf.Max(scale.Y, scale.Z));

        switch (Shape)
        {
            case BrushShape.Sphere:
            {
                DebugDraw.DrawWireSphere(
                    new BoundingSphere(pos, Radius * maxScale),
                    mainColor
                );

                DebugDraw.DrawWireSphere(
                    new BoundingSphere(pos, (Radius - Falloff) * maxScale),
                    falloffColor
                );
                break;
            }
            case BrushShape.Box:
            {
                Vector3 extents = Vector3.One * Radius;
                BoundingBox localBox = new BoundingBox(-extents, extents);

                OrientedBoundingBox obb = new OrientedBoundingBox(localBox);
                obb.Transform(ref t);

                DebugDraw.DrawWireBox(obb, mainColor);

                Vector3 falloffExt = Vector3.One * (Radius - Falloff);
                BoundingBox falloffBox = new BoundingBox(-falloffExt, falloffExt);
                OrientedBoundingBox falloffObb = new OrientedBoundingBox(falloffBox);
                falloffObb.Transform(ref t);

                DebugDraw.DrawWireBox(falloffObb, falloffColor);
                break;
            }
            case BrushShape.Capsule:
            {
                Vector3 dir = t.Forward;
                float halfLen = Radius * scale.Z;
                float rad = (Radius - Falloff) * scale.X;

                Vector3 a = pos + dir * halfLen;
                Vector3 b = pos - dir * halfLen;
                DebugDraw.DrawLine(a, b, mainColor);
                DebugDraw.DrawWireSphere(new BoundingSphere(a, rad), mainColor);
                DebugDraw.DrawWireSphere(new BoundingSphere(b, rad), mainColor);
                float falloffRad = Radius * scale.X;
                DebugDraw.DrawWireSphere(new BoundingSphere(a, falloffRad), falloffColor);
                DebugDraw.DrawWireSphere(new BoundingSphere(b, falloffRad), falloffColor);

                break;
            }
        }
    }
}
