using FlaxEngine;

public class SculptSpline : Script
{
    [Header("Spline Sculpt Settings")]
    public SculptMode Mode = SculptMode.Additive;
    public float Intensity = 1f;
    public float Width = 1200f;
    public float Falloff = 600f;
    [Range(0f, 1f)] public float Smoothing = 0.7f;

    [Tooltip("Optional. If empty, uses the Spline on this Actor.")]
    public Spline TargetSpline;

    public SculptSplineSample Bake()
    {
        var spline = TargetSpline ? TargetSpline : Actor as Spline;

        var sample = new SculptSplineSample
        {
            Mode = Mode,
            Intensity = Intensity,
            Width = Width,
            Falloff = Falloff,
            Smoothing = Smoothing
        };

        if (spline)
            spline.GetSplinePoints(out sample.Points);

        return sample;
    }
}
