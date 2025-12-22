using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlaxEngine;

[ExecuteInEditMode]
public class HybridProceduralTerrain : Script
{
    [Header("Global Settings")]
    public float MaxHeight = 4000f;
    public float NoiseScale = 0.01f;
    public float Roughness = 0.45f;

    [Header("Resolution & Scale")]
    [Tooltip("World-space distance between height samples (meters).")]
    public float SampleSpacing = 100f;
    [Tooltip("Terrain patch count (X,Y). Increase to cover a larger world area.")]
    public Int2 PatchGrid = new Int2(1, 1);

    [Header("Shape Control")]
    [Range(0, 1f)] public float GlobalSmoothing = 0.7f;
    public bool UseMountainMask = false;
    public float MountainMaskScale = 0.12f;
    public float RidgeSharpness = 2.2f;
    public bool UseTerrace = false;
    public float TerraceSteps = 5f;

    [Header("Directional Ridges")]
    public bool UseDirectionalWarp = true;
    public float WarpAngleDeg = 25f;
    public float WarpStrength = 25f;
    public float WarpFrequency = 0.002f;

    [Header("Curvature Detail")]
    public bool UseCurvatureDetail = true;
    [Range(0f, 1f)] public float RidgeGain = 0.35f;
    [Range(0f, 1f)] public float ValleyRelax = 0.2f;
    [Range(1, 3)] public int CurvatureIterations = 1;

    [Header("Natural Erosion")]
    public int Droplets = 60000;
    public float ErosionRate = 0.12f;
    public float DepositionRate = 0.05f;
    public float Gravity = 0.0f;

    [Header("Thermal Erosion")]
    public float ThermalStrength = 0.3f;
    public float AngleThreshold = 0.5f;

    [Header("Beach Blend")]
    public bool BeachEnabled = false;
    [Tooltip("Shoreline blend width from the outer edges (meters).")]
    public float BeachWidth = 8000f;
    [Tooltip("Edge easing curve. 1 = linear, >1 smoother.")]
    public float BeachSmoothness = 1.4f;

    [Header("Realtime")]
    public bool AutoRebuild = true;
    [Tooltip("Delay (seconds) before rebuilding after a change is detected.")]
    public float RebuildDebounce = 0.2f;

    private bool _isGenerating;
    private bool _pendingRebuild;
    private int _lastSignature = int.MinValue;
    private readonly System.Diagnostics.Stopwatch _changeTimer = new System.Diagnostics.Stopwatch();
    private readonly List<SculptBrush> _scratchBrushes = new List<SculptBrush>(32);
    private readonly List<SculptSpline> _scratchSplines = new List<SculptSpline>(16);

    [Button("Add Sculpt Brush")]
    public void CreateBrush()
    {
        var brushActor = new EmptyActor
        {
            Name = "SculptBrush",
            Parent = Actor
        };
        brushActor.Position = Actor.Position + new Vector3(0, 500, 0);
        brushActor.AddScript<SculptBrush>();
    }

    [Button("Add Sculpt Spline")]
    public void CreateSpline()
    {
        var splineActor = new Spline
        {
            Name = "SculptSpline",
            Parent = Actor
        };
        splineActor.Position = Actor.Position;

        Vector3 basePos = splineActor.Position;
        splineActor.AddSplinePoint(basePos + new Vector3(-500f, 0f, 0f));
        splineActor.AddSplinePoint(basePos + new Vector3(500f, 0f, 0f));

        splineActor.AddScript<SculptSpline>();
    }

    public override void OnStart()
    {
        _pendingRebuild = true;
        _changeTimer.Reset();
        _changeTimer.Start();
    }

    public override void OnUpdate()
    {
        if (!AutoRebuild)
            return;

        if (DetectChanges())
        {
            _pendingRebuild = true;
            _changeTimer.Restart();
        }

        if (_pendingRebuild && !_isGenerating && _changeTimer.Elapsed.TotalSeconds >= RebuildDebounce)
        {
            _pendingRebuild = false;
            Build();
        }
    }

    [Button("Build Procedural World")]
    public void Build()
    {
        if (_isGenerating)
            return;

        var terrain = Actor.GetChild<Terrain>();
        if (terrain == null)
            terrain = new Terrain { Parent = Actor, Name = "ProceduralTerrain" };

        terrain.Setup();

        int size = terrain.ChunkSize * Terrain.PatchEdgeChunksCount + 1;

        List<SculptBrush> brushes = new();
        CollectBrushes(Actor, brushes);
        List<SculptSplineSample> splines = new();
        CollectSplines(Actor, splines);

        _isGenerating = true;

        Task.Run(() =>
        {
            try
            {
                var patchMaps = TerrainPatchGenerator.Generate(
                    terrain.Position,
                    size,
                    PatchGrid,
                    SampleSpacing,
                    brushes.ToArray(),
                    splines.ToArray(),
                    this
                );

                ApplyPatches(terrain, patchMaps);
            }
            catch (Exception ex)
            {
                FlaxEngine.Debug.LogError(ex.ToString());
            }
            finally
            {
                _isGenerating = false;
            }
        });
    }

    private bool DetectChanges()
    {
        _scratchBrushes.Clear();
        CollectBrushes(Actor, _scratchBrushes);
        _scratchBrushes.Sort((a, b) => a.Actor.ID.CompareTo(b.Actor.ID));
        _scratchSplines.Clear();
        CollectSplines(Actor, _scratchSplines);
        _scratchSplines.Sort((a, b) => a.Actor.ID.CompareTo(b.Actor.ID));

        var hash = new HashCode();
        hash.Add(MaxHeight);
        hash.Add(NoiseScale);
        hash.Add(Roughness);
        hash.Add(GlobalSmoothing);
        hash.Add(UseMountainMask);
        hash.Add(MountainMaskScale);
        hash.Add(RidgeSharpness);
        hash.Add(UseTerrace);
        hash.Add(TerraceSteps);
        hash.Add(SampleSpacing);
        hash.Add(PatchGrid);
        hash.Add(UseDirectionalWarp);
        hash.Add(WarpAngleDeg);
        hash.Add(WarpStrength);
        hash.Add(WarpFrequency);
        hash.Add(UseCurvatureDetail);
        hash.Add(RidgeGain);
        hash.Add(ValleyRelax);
        hash.Add(CurvatureIterations);
        hash.Add(BeachEnabled);
        hash.Add(BeachWidth);
        hash.Add(BeachSmoothness);
        hash.Add(Droplets);
        hash.Add(ErosionRate);
        hash.Add(DepositionRate);
        hash.Add(Gravity);
        hash.Add(ThermalStrength);
        hash.Add(AngleThreshold);

        foreach (var brush in _scratchBrushes)
        {
            var t = brush.Actor.Transform;
            hash.Add(brush.Shape);
            hash.Add(brush.Mode);
            hash.Add(brush.Intensity);
            hash.Add(brush.Radius);
            hash.Add(brush.Falloff);
            hash.Add(t.Translation);
            hash.Add(t.Orientation);
            hash.Add(t.Scale);
        }

        foreach (var spline in _scratchSplines)
        {
            var sample = spline.Bake();
            hash.Add(sample.Mode);
            hash.Add(sample.Intensity);
            hash.Add(sample.Width);
            hash.Add(sample.Falloff);
            hash.Add(sample.Smoothing);

            if (sample.Points != null)
            {
                hash.Add(sample.Points.Length);
                for (int i = 0; i < sample.Points.Length; i++)
                    hash.Add(sample.Points[i]);
            }
        }

        int currentSignature = hash.ToHashCode();
        if (currentSignature == _lastSignature)
            return false;

        _lastSignature = currentSignature;
        return true;
    }

    private void CollectBrushes(Actor parent, List<SculptBrush> result)
    {
        foreach (var s in parent.Scripts)
            if (s is SculptBrush b && b.Enabled)
                result.Add(b);

        foreach (var c in parent.Children)
            CollectBrushes(c, result);
    }

    private void CollectSplines(Actor parent, List<SculptSpline> result)
    {
        foreach (var s in parent.Scripts)
            if (s is SculptSpline spline && spline.Enabled)
                result.Add(spline);

        foreach (var c in parent.Children)
            CollectSplines(c, result);
    }

    private void CollectSplines(Actor parent, List<SculptSplineSample> result)
    {
        foreach (var s in parent.Scripts)
            if (s is SculptSpline spline && spline.Enabled)
                result.Add(spline.Bake());

        foreach (var c in parent.Children)
            CollectSplines(c, result);
    }

    private void ApplyPatches(Terrain terrain, IReadOnlyList<PatchHeightmap> patchMaps)
    {
        Scripting.InvokeOnUpdate(() =>
        {
            foreach (var entry in patchMaps)
            {
                var coord = entry.Coord;
                if (!terrain.HasPatch(ref coord))
                    terrain.AddPatch(ref coord);

                terrain.SetupPatchHeightMap(ref coord, entry.Map, null, true);
            }
        });
    }
}
