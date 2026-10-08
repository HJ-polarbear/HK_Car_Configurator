using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class URPVolumetricSpotCone : MonoBehaviour
{
    [Header("Cone Settings")]
    [SerializeField] private bool enableCone = true;
    [SerializeField] private Material coneMaterial;
    [SerializeField] private int meshSegments = 64;
    [SerializeField] private int meshRings = 12;
    [SerializeField] private float rangeMultiplier = 1.0f;
    [SerializeField] private float angleMultiplier = 1.0f;

    [Header("Visual Settings")]
    [SerializeField] private bool useLightColor = true;
    [SerializeField] private Color coneColor = new Color(1.0f, 0.95f, 0.75f, 1.0f);
    [SerializeField] private float coneIntensity = 1.0f;
    [SerializeField] private float coneOpacity = 0.35f;
    [SerializeField] private float edgeSoftness = 1.5f;
    [SerializeField] private float lengthFade = 1.25f;
    [SerializeField] private float depthFadeDistance = 1.0f;

    [Header("Noise Settings")]
    [SerializeField] private bool useNoise = true;
    [SerializeField] private float noiseScale = 3.0f;
    [SerializeField] private float noiseSpeed = 0.15f;
    [SerializeField] private float noiseStrength = 0.25f;

    private const string ConeObjectName = "URP Volumetric Spot Cone";

    private Light targetLight;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh coneMesh;

    private float lastRange;
    private float lastAngle;
    private int lastSegments;
    private int lastRings;

    private void OnEnable()
    {
        targetLight = GetComponent<Light>();

        if (targetLight != null && targetLight.type != LightType.Spot)
        {
            Debug.LogWarning($"{nameof(URPVolumetricSpotCone)} works best with Spot Light.", this);
        }

        EnsureConeObject();
        RebuildMeshIfNeeded(true);
        ApplyMaterialProperties();
    }

    private void OnDisable()
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        EnsureConeObject();

        if (meshRenderer != null)
        {
            meshRenderer.enabled = enableCone && targetLight != null && targetLight.enabled;
        }

        RebuildMeshIfNeeded(false);
        ApplyMaterialProperties();
    }

    private void EnsureConeObject()
    {
        Transform coneTransform = transform.Find(ConeObjectName);

        if (coneTransform == null)
        {
            GameObject coneObject = new GameObject(ConeObjectName);
            coneObject.transform.SetParent(transform, false);
            coneObject.transform.localPosition = Vector3.zero;
            coneObject.transform.localRotation = Quaternion.identity;
            coneObject.transform.localScale = Vector3.one;

            meshFilter = coneObject.AddComponent<MeshFilter>();
            meshRenderer = coneObject.AddComponent<MeshRenderer>();
        }
        else
        {
            meshFilter = coneTransform.GetComponent<MeshFilter>();
            meshRenderer = coneTransform.GetComponent<MeshRenderer>();

            if (meshFilter == null)
            {
                meshFilter = coneTransform.gameObject.AddComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = coneTransform.gameObject.AddComponent<MeshRenderer>();
            }
        }

        if (meshRenderer != null)
        {
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            meshRenderer.allowOcclusionWhenDynamic = false;

            if (coneMaterial != null)
            {
                meshRenderer.sharedMaterial = coneMaterial;
            }
        }
    }

    private void RebuildMeshIfNeeded(bool force)
    {
        if (targetLight == null || meshFilter == null)
        {
            return;
        }

        float currentRange = Mathf.Max(0.01f, targetLight.range * rangeMultiplier);
        float currentAngle = Mathf.Clamp(targetLight.spotAngle * angleMultiplier, 1.0f, 179.0f);

        int currentSegments = Mathf.Clamp(meshSegments, 8, 256);
        int currentRings = Mathf.Clamp(meshRings, 2, 64);

        bool needsRebuild =
            force ||
            coneMesh == null ||
            !Mathf.Approximately(currentRange, lastRange) ||
            !Mathf.Approximately(currentAngle, lastAngle) ||
            currentSegments != lastSegments ||
            currentRings != lastRings;

        if (!needsRebuild)
        {
            return;
        }

        coneMesh = BuildConeMesh(currentRange, currentAngle, currentSegments, currentRings);
        meshFilter.sharedMesh = coneMesh;

        lastRange = currentRange;
        lastAngle = currentAngle;
        lastSegments = currentSegments;
        lastRings = currentRings;
    }

    private Mesh BuildConeMesh(float length, float angle, int segments, int rings)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Volumetric Spot Cone";

        float radius = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * length;

        int vertexCount = 1 + rings * segments;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.0f);

        int vertexIndex = 1;

        for (int ring = 1; ring <= rings; ring++)
        {
            float t = ring / (float)rings;
            float ringZ = length * t;
            float ringRadius = radius * t;

            for (int segment = 0; segment < segments; segment++)
            {
                float angle01 = segment / (float)segments;
                float radian = angle01 * Mathf.PI * 2.0f;

                float x = Mathf.Cos(radian) * ringRadius;
                float y = Mathf.Sin(radian) * ringRadius;

                vertices[vertexIndex] = new Vector3(x, y, ringZ);
                uvs[vertexIndex] = new Vector2(angle01, t);

                vertexIndex++;
            }
        }

        int triangleCount = segments * 3 + (rings - 1) * segments * 6;
        int[] triangles = new int[triangleCount];

        int triangleIndex = 0;

        for (int segment = 0; segment < segments; segment++)
        {
            int current = 1 + segment;
            int next = 1 + ((segment + 1) % segments);

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;
        }

        for (int ring = 1; ring < rings; ring++)
        {
            int currentRingStart = 1 + (ring - 1) * segments;
            int nextRingStart = 1 + ring * segments;

            for (int segment = 0; segment < segments; segment++)
            {
                int current = currentRingStart + segment;
                int currentNext = currentRingStart + ((segment + 1) % segments);

                int next = nextRingStart + segment;
                int nextNext = nextRingStart + ((segment + 1) % segments);

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = currentNext;
                triangles[triangleIndex++] = nextNext;

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = nextNext;
                triangles[triangleIndex++] = next;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void ApplyMaterialProperties()
    {
        if (targetLight == null || meshRenderer == null || meshRenderer.sharedMaterial == null)
        {
            return;
        }

        Material material = meshRenderer.sharedMaterial;

        Color finalColor = useLightColor ? targetLight.color : coneColor;

        float currentRange = Mathf.Max(0.01f, targetLight.range * rangeMultiplier);
        float currentAngle = Mathf.Clamp(targetLight.spotAngle * angleMultiplier, 1.0f, 179.0f);

        material.SetColor("_ConeColor", finalColor);
        material.SetFloat("_ConeLength", currentRange);
        material.SetFloat("_ConeAngle", currentAngle);
        material.SetFloat("_ConeIntensity", coneIntensity);
        material.SetFloat("_ConeOpacity", coneOpacity);
        material.SetFloat("_EdgeSoftness", edgeSoftness);
        material.SetFloat("_LengthFade", lengthFade);
        material.SetFloat("_DepthFadeDistance", depthFadeDistance);

        material.SetFloat("_NoiseToggle", useNoise ? 1.0f : 0.0f);
        material.SetFloat("_NoiseScale", noiseScale);
        material.SetFloat("_NoiseSpeed", noiseSpeed);
        material.SetFloat("_NoiseStrength", noiseStrength);
    }
}