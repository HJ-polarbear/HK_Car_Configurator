using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The volumetric fog renderer feature.
/// Handles material lifecycle and enqueues the volumetric fog render pass.
/// </summary>
[Tooltip("Adds support to render volumetric fog.")]
[DisallowMultipleRendererFeature("Volumetric Fog")]
public sealed class VolumetricFogRendererFeature : ScriptableRendererFeature
{
    #region Private Attributes

    [HideInInspector]
    [SerializeField] private Shader downsampleDepthShader;
    [HideInInspector]
    [SerializeField] private Shader volumetricFogShader;

    private Material downsampleDepthMaterial;
    private Material volumetricFogMaterial;
    private VolumetricFogRenderPass volumetricFogRenderPass;

    #endregion

    #region Scriptable Renderer Feature Methods

    /// <summary>
    /// Initialize resources and the render pass.
    /// </summary>
    public override void Create()
    {
        ValidateResourcesForVolumetricFogRenderPass(true);
        volumetricFogRenderPass = new VolumetricFogRenderPass(downsampleDepthMaterial, volumetricFogMaterial);
    }

    /// <summary>
    /// Configures and enqueues the render pass into the pipeline.
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Check if post-processing is enabled for the camera
        bool isPostProcessEnabled = renderingData.postProcessingEnabled && renderingData.cameraData.postProcessEnabled;
        bool shouldAddVolumetricFogRenderPass = isPostProcessEnabled && ShouldAddVolumetricFogRenderPass(renderingData.cameraData.cameraType);

        if (shouldAddVolumetricFogRenderPass)
        {
            // Ensure the pass has access to the camera depth texture
            volumetricFogRenderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(volumetricFogRenderPass);
        }
    }

    /// <summary>
    /// Cleans up resources when the feature is destroyed or disabled.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Note: Render Graph handles pass-internal texture disposal automatically.
        // We only need to destroy materials created by the feature.
        CoreUtils.Destroy(downsampleDepthMaterial);
        CoreUtils.Destroy(volumetricFogMaterial);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Validates and initializes shaders and materials.
    /// </summary>
    private bool ValidateResourcesForVolumetricFogRenderPass(bool forceRefresh)
    {
        if (forceRefresh)
        {
#if UNITY_EDITOR
            // Find shaders if they are not assigned
            if (downsampleDepthShader == null) downsampleDepthShader = Shader.Find("Hidden/DownsampleDepth");
            if (volumetricFogShader == null) volumetricFogShader = Shader.Find("Hidden/VolumetricFog");
#endif
            // Re-create materials
            if (downsampleDepthMaterial == null && downsampleDepthShader != null)
                downsampleDepthMaterial = CoreUtils.CreateEngineMaterial(downsampleDepthShader);

            if (volumetricFogMaterial == null && volumetricFogShader != null)
                volumetricFogMaterial = CoreUtils.CreateEngineMaterial(volumetricFogShader);
        }

        bool okDepth = downsampleDepthShader != null && downsampleDepthMaterial != null;
        bool okVolumetric = volumetricFogShader != null && volumetricFogMaterial != null;

        return okDepth && okVolumetric;
    }

    /// <summary>
    /// Determines if the fog pass should be executed based on camera type and volume settings.
    /// </summary>
    private bool ShouldAddVolumetricFogRenderPass(CameraType cameraType)
    {
        var stack = VolumeManager.instance.stack;
        if (stack == null) return false;

        VolumetricFogVolumeComponent fogVolume = stack.GetComponent<VolumetricFogVolumeComponent>();

        bool isVolumeOk = fogVolume != null && fogVolume.IsActive();
        bool isCameraOk = cameraType != CameraType.Preview && cameraType != CameraType.Reflection;
        bool areResourcesOk = ValidateResourcesForVolumetricFogRenderPass(false);

        return isActive && isVolumeOk && isCameraOk && areResourcesOk;
    }

    #endregion
}
