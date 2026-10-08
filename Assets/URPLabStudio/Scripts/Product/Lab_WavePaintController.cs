namespace URPLabStudio
{
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Lab_WavePaintController : MonoBehaviour
{
    [Serializable]
    public sealed class PaintPreset
    {
        public string displayName;
        public Color color = Color.white;
    }

    [Serializable]
    public sealed class PaintTarget
    {
        public Renderer renderer;
        public int materialSlot;
        public Material detectedMaterial;
    }

    sealed class OriginalMaterial
    {
        public Renderer renderer;
        public int materialSlot;
        public Material material;
    }

    static readonly int MainColorId = Shader.PropertyToID("_MainColor");
    static readonly int TargetColorId = Shader.PropertyToID("_TargetColor");
    static readonly int StartPosId = Shader.PropertyToID("_StartPos");
    static readonly int WaveOffsetId = Shader.PropertyToID("_WaveOffset");
    static readonly int WaveLengthId = Shader.PropertyToID("_WaveLength");
    static readonly int WaveStrengthId = Shader.PropertyToID("_WaveStrenth");
    static readonly int PaintColorId = Shader.PropertyToID("Color_CF7B3627");

    [Header("Vehicle")]
    [SerializeField] Transform vehicleRoot;
    [SerializeField] List<PaintTarget> paintTargets = new List<PaintTarget>();
    [SerializeField] string[] paintMaterialPrefixes = { "Lab_Color_", "Lab_Pearl_" };

    [Header("Wave Material")]
    [SerializeField] Material waveMaterialTemplate;
    [SerializeField] float startOffset = -5.5f;
    [SerializeField] float endOffset = 50f;
    [SerializeField, Min(.01f)] float waveSpeed = 3.8f;

    [Header("Scale Compensation")]
    [SerializeField] bool compensateTransformScale = true;
    [SerializeField, Min(.0001f)] float referenceScale = 1f;
    [SerializeField, Min(.0001f)] float minimumScaleFactor = .01f;

    [Header("Paint Presets")]
    [SerializeField] List<PaintPreset> paintPresets = new List<PaintPreset>();

    readonly List<OriginalMaterial> originals = new List<OriginalMaterial>();
    Material runtimeWaveMaterial;
    Coroutine waveRoutine;
    Bounds bodyBounds;
    bool hasBodyBounds;
    float adjustedStartOffset;
    float adjustedEndOffset;
    float sourceWaveLength;
    float sourceWaveStrength;
    float appliedScaleFactor = 1f;

    public Transform VehicleRoot => vehicleRoot;
    public Material WaveMaterialTemplate => waveMaterialTemplate;
    public IReadOnlyList<PaintTarget> PaintTargets => paintTargets;
    public IReadOnlyList<PaintPreset> PaintPresets => paintPresets;
    public float WaveSpeed
    {
        get => waveSpeed;
        set => waveSpeed = Mathf.Max(.01f, value);
    }

    void Reset()
    {
        vehicleRoot = transform.root;
        EnsureDefaultPresets();
    }

    void Awake()
    {
        if (vehicleRoot == null)
            vehicleRoot = transform.root;
        EnsureDefaultPresets();
        PrepareRuntimeMaterial();
    }

    void OnDestroy()
    {
        RestoreOriginalMaterials();
        if (runtimeWaveMaterial != null)
            Destroy(runtimeWaveMaterial);
    }

    public void Configure(Transform root, Material template)
    {
        vehicleRoot = root != null ? root : transform.root;
        waveMaterialTemplate = template;
        EnsureDefaultPresets();
    }

    public int ScanPaintTargets(bool includeInactive = false)
    {
        paintTargets.Clear();
        hasBodyBounds = false;
        Transform root = vehicleRoot != null ? vehicleRoot : transform.root;
        if (root == null)
            return 0;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
        Dictionary<string, int> usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Material> materials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (!IsPaintCandidate(material))
                    continue;
                string name = CleanMaterialName(material.name);
                usage.TryGetValue(name, out int count);
                usage[name] = count + 1;
                materials[name] = material;
            }
        }

        if (usage.Count == 0)
            return 0;
        string dominantName = usage.OrderByDescending(pair => pair.Value).First().Key;
        foreach (Renderer renderer in renderers)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;
            for (int index = 0; index < sharedMaterials.Length; index++)
            {
                Material material = sharedMaterials[index];
                if (material == null || !string.Equals(CleanMaterialName(material.name), dominantName,
                        StringComparison.OrdinalIgnoreCase))
                    continue;
                paintTargets.Add(new PaintTarget
                {
                    renderer = renderer,
                    materialSlot = index,
                    detectedMaterial = materials[dominantName]
                });
                Encapsulate(renderer.bounds);
            }
        }
        return paintTargets.Count;
    }

    public void AutoFitWaveRange()
    {
        RefreshBoundsFromTargets();
        if (!hasBodyBounds)
            return;
        float radius = Mathf.Max(.01f, bodyBounds.extents.magnitude);
        float scale = ResolveAverageScale();
        startOffset = -.1f * radius / scale;
        endOffset = 2.4f * radius / scale;
    }

    public string ValidateConfiguration()
    {
        List<string> issues = new List<string>();
        if (vehicleRoot == null)
            issues.Add("Vehicle Root is not assigned.");
        if (waveMaterialTemplate == null)
            issues.Add("Wave Material Template is not assigned.");
        if (paintTargets.Count == 0)
            issues.Add("No paint targets are stored. Run Scan Paint Targets.");
        foreach (PaintTarget target in paintTargets)
        {
            if (target == null || target.renderer == null)
            {
                issues.Add("A paint target has no Renderer.");
                continue;
            }
            if (target.materialSlot < 0 || target.materialSlot >= target.renderer.sharedMaterials.Length)
                issues.Add($"Material Slot is invalid on {target.renderer.name}.");
        }
        return issues.Count == 0 ? "Setup is valid." : string.Join("\n", issues);
    }

    public bool ApplyPreset(int index)
    {
        if (index < 0 || index >= paintPresets.Count || paintPresets[index] == null)
            return false;
        ApplyColorWithWave(paintPresets[index].color);
        return true;
    }

    public void ApplyColorWithWave(Color targetColor)
    {
        if (!PrepareRuntimeMaterial())
            return;
        if (waveRoutine != null)
            StopCoroutine(waveRoutine);
        runtimeWaveMaterial.SetVector(StartPosId, ResolveWaveStartPoint());
        runtimeWaveMaterial.SetColor(TargetColorId, targetColor);
        runtimeWaveMaterial.SetFloat(WaveOffsetId, adjustedStartOffset);
        Lab_ConfiguratorAudio.Instance?.PlayColorChange();
        waveRoutine = StartCoroutine(AnimateWave(targetColor));
    }

    bool PrepareRuntimeMaterial()
    {
        if (runtimeWaveMaterial != null && originals.Count > 0)
            return true;
        if (waveMaterialTemplate == null)
        {
            Debug.LogError("URP Wave Paint: Wave Material Template is not assigned.", this);
            return false;
        }
        if (paintTargets.Count == 0 && ScanPaintTargets() == 0)
        {
            Debug.LogWarning("URP Wave Paint: no compatible paint material was found under Vehicle Root.", this);
            return false;
        }

        PaintTarget sourceTarget = paintTargets.FirstOrDefault(target =>
            target != null && target.detectedMaterial != null);
        if (sourceTarget == null)
            return false;
        Material sourceMaterial = sourceTarget.detectedMaterial;
        runtimeWaveMaterial = new Material(waveMaterialTemplate)
        {
            name = waveMaterialTemplate.name + " (Runtime)"
        };
        sourceWaveLength = runtimeWaveMaterial.HasProperty(WaveLengthId)
            ? runtimeWaveMaterial.GetFloat(WaveLengthId)
            : .5f;
        sourceWaveStrength = runtimeWaveMaterial.HasProperty(WaveStrengthId)
            ? runtimeWaveMaterial.GetFloat(WaveStrengthId)
            : .5f;
        CopyCarPaintAppearance(sourceMaterial, runtimeWaveMaterial);

        Color initialColor = ReadPaintColor(sourceMaterial);
        originals.Clear();
        hasBodyBounds = false;
        foreach (PaintTarget target in paintTargets)
        {
            if (target == null || target.renderer == null)
                continue;
            Material[] materials = target.renderer.sharedMaterials;
            if (target.materialSlot < 0 || target.materialSlot >= materials.Length)
                continue;
            originals.Add(new OriginalMaterial
            {
                renderer = target.renderer,
                materialSlot = target.materialSlot,
                material = materials[target.materialSlot]
            });
            materials[target.materialSlot] = runtimeWaveMaterial;
            target.renderer.sharedMaterials = materials;
            Encapsulate(target.renderer.bounds);
        }

        if (originals.Count == 0)
        {
            Destroy(runtimeWaveMaterial);
            runtimeWaveMaterial = null;
            return false;
        }
        runtimeWaveMaterial.SetColor(MainColorId, initialColor);
        runtimeWaveMaterial.SetColor(TargetColorId, initialColor);
        ApplyScaleCompensation();
        runtimeWaveMaterial.SetFloat(WaveOffsetId, adjustedStartOffset);
        return true;
    }

    void RestoreOriginalMaterials()
    {
        foreach (OriginalMaterial original in originals)
        {
            if (original.renderer == null)
                continue;
            Material[] materials = original.renderer.sharedMaterials;
            if (original.materialSlot < 0 || original.materialSlot >= materials.Length)
                continue;
            if (materials[original.materialSlot] != runtimeWaveMaterial)
                continue;
            materials[original.materialSlot] = original.material;
            original.renderer.sharedMaterials = materials;
        }
        originals.Clear();
    }

    IEnumerator AnimateWave(Color targetColor)
    {
        float elapsed = 0f;
        float travelDistance = Mathf.Abs(adjustedEndOffset - adjustedStartOffset);
        float duration = Mathf.Max(.05f, travelDistance / Mathf.Max(.01f, waveSpeed));
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            runtimeWaveMaterial.SetFloat(WaveOffsetId, Mathf.Lerp(adjustedStartOffset, adjustedEndOffset, t));
            yield return null;
        }
        runtimeWaveMaterial.SetFloat(WaveOffsetId, adjustedStartOffset);
        runtimeWaveMaterial.SetColor(MainColorId, targetColor);
        runtimeWaveMaterial.SetColor(TargetColorId, targetColor);
        waveRoutine = null;
    }

    Vector3 ResolveWaveStartPoint()
    {
        if (!hasBodyBounds)
            return vehicleRoot != null ? vehicleRoot.position : transform.position;
        Camera viewCamera = Camera.main;
        if (viewCamera != null)
        {
            Vector3 visibleSurface = bodyBounds.ClosestPoint(viewCamera.transform.position);
            return visibleSurface + Vector3.up * bodyBounds.extents.y * .05f;
        }
        return bodyBounds.center + Vector3.up * bodyBounds.extents.y * .15f;
    }

    void ApplyScaleCompensation()
    {
        appliedScaleFactor = compensateTransformScale
            ? Mathf.Max(minimumScaleFactor, ResolveAverageScale() / referenceScale)
            : 1f;
        adjustedStartOffset = startOffset * appliedScaleFactor;
        adjustedEndOffset = endOffset * appliedScaleFactor;
        if (runtimeWaveMaterial.HasProperty(WaveLengthId))
            runtimeWaveMaterial.SetFloat(WaveLengthId, sourceWaveLength / appliedScaleFactor);
        if (runtimeWaveMaterial.HasProperty(WaveStrengthId))
            runtimeWaveMaterial.SetFloat(WaveStrengthId, sourceWaveStrength);
    }

    float ResolveAverageScale()
    {
        List<Renderer> renderers = paintTargets
            .Where(target => target != null && target.renderer != null)
            .Select(target => target.renderer)
            .Distinct()
            .ToList();
        if (renderers.Count == 0)
            return 1f;
        float sum = 0f;
        foreach (Renderer renderer in renderers)
        {
            Vector3 scale = renderer.transform.lossyScale;
            sum += (Mathf.Abs(scale.x) + Mathf.Abs(scale.y) + Mathf.Abs(scale.z)) / 3f;
        }
        return Mathf.Max(.0001f, sum / renderers.Count);
    }

    void RefreshBoundsFromTargets()
    {
        hasBodyBounds = false;
        foreach (PaintTarget target in paintTargets)
            if (target != null && target.renderer != null)
                Encapsulate(target.renderer.bounds);
    }

    void Encapsulate(Bounds bounds)
    {
        if (!hasBodyBounds)
        {
            bodyBounds = bounds;
            hasBodyBounds = true;
        }
        else
        {
            bodyBounds.Encapsulate(bounds);
        }
    }

    bool IsPaintCandidate(Material material)
    {
        if (material == null || material.shader == null)
            return false;
        string cleanName = CleanMaterialName(material.name);
        bool prefixMatch = paintMaterialPrefixes != null && paintMaterialPrefixes.Any(prefix =>
            !string.IsNullOrWhiteSpace(prefix) && cleanName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (!prefixMatch)
            return false;
        return material.HasProperty(PaintColorId) || material.HasProperty("_BaseColor") || material.HasProperty("_Color");
    }

    static Color ReadPaintColor(Material material)
    {
        if (material.HasProperty(PaintColorId))
            return material.GetColor(PaintColorId);
        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");
        return Color.white;
    }

    void EnsureDefaultPresets()
    {
        if (paintPresets.Count > 0)
            return;
        paintPresets.Add(new PaintPreset { displayName = "Pearl White", color = Color.white });
        paintPresets.Add(new PaintPreset { displayName = "Pearl Silver", color = new Color(.55f, .58f, .62f) });
        paintPresets.Add(new PaintPreset { displayName = "Dark Gray", color = new Color(.05f, .06f, .07f) });
        paintPresets.Add(new PaintPreset { displayName = "Cherry Red", color = new Color(.7f, .03f, .03f) });
        paintPresets.Add(new PaintPreset { displayName = "Electric Blue", color = new Color(.02f, .22f, .75f) });
        paintPresets.Add(new PaintPreset { displayName = "Pearl Yellow", color = new Color(.95f, .55f, .08f) });
    }

    static void CopyCarPaintAppearance(Material source, Material destination)
    {
        CopyFloat(source, destination, "Vector1_1E073C19", "_Smoothness_Intensity");
        CopyFloat(source, destination, "Vector1_856C34E2", "_Metallic_Intensity");
        CopyFloat(source, destination, "Vector1_2B557478", "_Occlusion_Intensity");
        CopyFloat(source, destination, "Vector1_2BC3876C", "_Coat_Smoothness");
        CopyFloat(source, destination, "Vector1_49C052DA", "_Coat_Mask");
        CopyFloat(source, destination, "Vector1_D873B436", "_Flakes_Intensity");
        CopyFloat(source, destination, "Boolean_1171460D", "_Make_Triplanar");
        CopyFloat(source, destination, "Boolean_9F8B3345", "_Make_Triplanar_WorldSpace");
        CopyFloat(source, destination, "Vector1_F7E22660", "_Triplanar_Blend");
        CopyTexture(source, destination, "Texture2D_F5401D3B", "_Flakes_Normal");
        CopyVector(source, destination, "Vector2_AB87CEC4", "_Flakes_Tiling");
    }

    static void CopyFloat(Material source, Material destination, string sourceName, string destinationName)
    {
        if (source.HasProperty(sourceName) && destination.HasProperty(destinationName))
            destination.SetFloat(destinationName, source.GetFloat(sourceName));
    }

    static void CopyVector(Material source, Material destination, string sourceName, string destinationName)
    {
        if (source.HasProperty(sourceName) && destination.HasProperty(destinationName))
            destination.SetVector(destinationName, source.GetVector(sourceName));
    }

    static void CopyTexture(Material source, Material destination, string sourceName, string destinationName)
    {
        if (source.HasProperty(sourceName) && destination.HasProperty(destinationName))
            destination.SetTexture(destinationName, source.GetTexture(sourceName));
    }

    static string CleanMaterialName(string materialName)
    {
        return materialName.Replace(" (Instance)", string.Empty).Replace(" (Runtime)", string.Empty);
    }
}
}
