using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class URPFakeAreaLightPreview : MonoBehaviour
{
    [Header("Preview")]
    public bool enableFakePreview = true;
    public bool useLightColor = true;
    public Material previewMaterial;

    [Header("Shape")]
    public Vector2 size = new Vector2(2.0f, 1.0f);
    public float forwardOffset = 0.01f;

    [Header("Core Visual")]
    public Color color = new Color(1.0f, 0.85f, 0.55f, 1.0f);
    public float intensity = 2.0f;
    public float opacity = 0.35f;
    public float edgeSoftness = 2.5f;
    public float centerBoost = 1.5f;

    [Header("Soft Glow Spread")]
    public bool useSoftGlow = true;
    public float glowSizeMultiplier = 3.0f;
    public float glowForwardOffset = 0.005f;
    public float glowIntensity = 1.25f;
    public float glowOpacity = 0.08f;
    public float glowEdgeSoftness = 4.0f;
    public float glowCenterBoost = 1.25f;

    [Header("Noise")]
    public bool useNoise = true;
    public float noiseScale = 4.0f;
    public float noiseSpeed = 0.1f;
    public float noiseStrength = 0.15f;

    [Header("Bake Workflow")]
    public bool hideFakeWhenBaking = false;

    private const string CoreObjectName = "URP Fake Area Light Preview";
    private const string GlowObjectName = "URP Fake Area Light Soft Glow";

    private Light targetLight;

    private MeshFilter coreMeshFilter;
    private MeshRenderer coreMeshRenderer;

    private MeshFilter glowMeshFilter;
    private MeshRenderer glowMeshRenderer;

    private Mesh coreMesh;
    private Mesh glowMesh;

    private Vector2 lastCoreSize;
    private Vector2 lastGlowSize;

    private MaterialPropertyBlock corePropertyBlock;
    private MaterialPropertyBlock glowPropertyBlock;

    private void OnEnable()
    {
        targetLight = GetComponent<Light>();

        EnsureObjects();
        RebuildMeshesIfNeeded(true);
        ApplyMaterialProperties();
    }

    private void OnDisable()
    {
        SetRendererEnabled(coreMeshRenderer, false);
        SetRendererEnabled(glowMeshRenderer, false);
    }

    private void Update()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        SyncFromLight();
        EnsureObjects();
        RebuildMeshesIfNeeded(false);
        ApplyMaterialProperties();

        bool visible = enableFakePreview && !hideFakeWhenBaking;

        SetRendererEnabled(coreMeshRenderer, visible);
        SetRendererEnabled(glowMeshRenderer, visible && useSoftGlow);
    }

    private void SyncFromLight()
    {
        if (targetLight == null)
        {
            return;
        }

        if (targetLight.type == LightType.Rectangle)
        {
            size = targetLight.areaSize;
        }

        if (useLightColor)
        {
            color = targetLight.color;
        }
    }

    private void EnsureObjects()
    {
        EnsureSingleObject(
            CoreObjectName,
            forwardOffset,
            ref coreMeshFilter,
            ref coreMeshRenderer
        );

        EnsureSingleObject(
            GlowObjectName,
            glowForwardOffset,
            ref glowMeshFilter,
            ref glowMeshRenderer
        );
    }

    private void EnsureSingleObject(
        string objectName,
        float localForwardOffset,
        ref MeshFilter meshFilter,
        ref MeshRenderer meshRenderer)
    {
        Transform child = transform.Find(objectName);

        if (child == null)
        {
            GameObject previewObject = new GameObject(objectName);
            previewObject.transform.SetParent(transform, false);
            previewObject.transform.localRotation = Quaternion.identity;
            previewObject.transform.localScale = Vector3.one;

            child = previewObject.transform;

            meshFilter = previewObject.AddComponent<MeshFilter>();
            meshRenderer = previewObject.AddComponent<MeshRenderer>();
        }
        else
        {
            meshFilter = child.GetComponent<MeshFilter>();
            meshRenderer = child.GetComponent<MeshRenderer>();

            if (meshFilter == null)
            {
                meshFilter = child.gameObject.AddComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = child.gameObject.AddComponent<MeshRenderer>();
            }
        }

        child.localPosition = new Vector3(0.0f, 0.0f, localForwardOffset);

        if (meshRenderer != null)
        {
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            meshRenderer.allowOcclusionWhenDynamic = false;

            if (previewMaterial != null)
            {
                meshRenderer.sharedMaterial = previewMaterial;
            }
        }
    }

    private void RebuildMeshesIfNeeded(bool force)
    {
        Vector2 coreSize = new Vector2(
            Mathf.Max(0.01f, size.x),
            Mathf.Max(0.01f, size.y)
        );

        Vector2 glowSize = coreSize * Mathf.Max(1.0f, glowSizeMultiplier);

        if (force || coreMesh == null || coreSize != lastCoreSize)
        {
            coreMesh = BuildQuadMesh(coreSize, "Generated Fake Area Light Core");
            if (coreMeshFilter != null)
            {
                coreMeshFilter.sharedMesh = coreMesh;
            }

            lastCoreSize = coreSize;
        }

        if (force || glowMesh == null || glowSize != lastGlowSize)
        {
            glowMesh = BuildQuadMesh(glowSize, "Generated Fake Area Light Soft Glow");
            if (glowMeshFilter != null)
            {
                glowMeshFilter.sharedMesh = glowMesh;
            }

            lastGlowSize = glowSize;
        }
    }

    private Mesh BuildQuadMesh(Vector2 quadSize, string meshName)
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName;

        float halfX = quadSize.x * 0.5f;
        float halfY = quadSize.y * 0.5f;

        Vector3[] vertices =
        {
            new Vector3(-halfX, -halfY, 0.0f),
            new Vector3( halfX, -halfY, 0.0f),
            new Vector3(-halfX,  halfY, 0.0f),
            new Vector3( halfX,  halfY, 0.0f)
        };

        Vector2[] uvs =
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f)
        };

        int[] triangles =
        {
            0, 2, 1,
            2, 3, 1
        };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void ApplyMaterialProperties()
    {
        if (corePropertyBlock == null)
        {
            corePropertyBlock = new MaterialPropertyBlock();
        }

        if (glowPropertyBlock == null)
        {
            glowPropertyBlock = new MaterialPropertyBlock();
        }

        ApplyPropertiesToRenderer(
            coreMeshRenderer,
            corePropertyBlock,
            color,
            intensity,
            opacity,
            edgeSoftness,
            centerBoost
        );

        ApplyPropertiesToRenderer(
            glowMeshRenderer,
            glowPropertyBlock,
            color,
            glowIntensity,
            glowOpacity,
            glowEdgeSoftness,
            glowCenterBoost
        );
    }

    private void ApplyPropertiesToRenderer(
        MeshRenderer targetRenderer,
        MaterialPropertyBlock propertyBlock,
        Color targetColor,
        float targetIntensity,
        float targetOpacity,
        float targetEdgeSoftness,
        float targetCenterBoost)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor("_AreaColor", targetColor);
        propertyBlock.SetFloat("_Intensity", targetIntensity);
        propertyBlock.SetFloat("_Opacity", targetOpacity);
        propertyBlock.SetFloat("_EdgeSoftness", targetEdgeSoftness);
        propertyBlock.SetFloat("_CenterBoost", targetCenterBoost);

        propertyBlock.SetFloat("_NoiseToggle", useNoise ? 1.0f : 0.0f);
        propertyBlock.SetFloat("_NoiseScale", noiseScale);
        propertyBlock.SetFloat("_NoiseSpeed", noiseSpeed);
        propertyBlock.SetFloat("_NoiseStrength", noiseStrength);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetRendererEnabled(MeshRenderer targetRenderer, bool enabledState)
    {
        if (targetRenderer != null)
        {
            targetRenderer.enabled = enabledState;
        }
    }
}