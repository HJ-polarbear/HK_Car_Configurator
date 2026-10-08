using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Volume component for the Volumetric Fog refactored for Unity 6.4.
/// Optimized for the modern Render Graph system and newer URP Core API.
/// </summary>
[System.Serializable]
[VolumeComponentMenu("Custom/Volumetric Fog")]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class VolumetricFogVolumeComponent : VolumeComponent
{
    #region Public Parameters

    [Header("Distances")]
    public ClampedFloatParameter distance = new ClampedFloatParameter(128.0f, 16.0f, 512.0f);
    public FloatParameter baseHeight = new FloatParameter(0.0f);
    public FloatParameter maximumHeight = new FloatParameter(50.0f);

    [Header("Lighting")]
    public ClampedFloatParameter density = new ClampedFloatParameter(0.2f, 0.0f, 1.0f);
    public MinFloatParameter attenuationDistance = new MinFloatParameter(128.0f, 0.05f);
    public ColorParameter tint = new ColorParameter(Color.white, true, false, true, false);

    [Header("Main Light")]
    public ClampedFloatParameter mainLightAnisotropy = new ClampedFloatParameter(0.4f, 0.0f, 0.99f);
    public ClampedFloatParameter mainLightScattering = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);

    [Header("Additional Lights")]
    public ClampedFloatParameter additionalLightsAnisotropy = new ClampedFloatParameter(0.25f, 0.0f, 0.99f);
    public ClampedFloatParameter additionalLightsScattering = new ClampedFloatParameter(1.0f, 0.0f, 32.0f);
    public ClampedFloatParameter additionalLightsRadius = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

    [Header("Local Box Mask")]
    public BoolParameter useLocalBox = new BoolParameter(false);
    public NoInterpVector3Parameter localBoxCenter = new NoInterpVector3Parameter(Vector3.zero);
    public NoInterpVector3Parameter localBoxSize = new NoInterpVector3Parameter(new Vector3(4f, 3f, 4f));
    public ClampedFloatParameter localBoxFeather = new ClampedFloatParameter(0.35f, 0.001f, 10f);

    [Header("Performance & Quality")]
    public ClampedIntParameter maxSteps = new ClampedIntParameter(64, 8, 256);
    public ClampedIntParameter blurIterations = new ClampedIntParameter(2, 1, 4);
    public BoolParameter activeFog = new BoolParameter(false);

    #endregion

    public bool IsActive() => activeFog.value;

    void OnValidate()
    {
        maximumHeight.value = Mathf.Max(baseHeight.value, maximumHeight.value);

        Vector3 size = localBoxSize.value;
        size.x = Mathf.Max(0.001f, size.x);
        size.y = Mathf.Max(0.001f, size.y);
        size.z = Mathf.Max(0.001f, size.z);
        localBoxSize.value = size;

        localBoxFeather.value = Mathf.Max(0.001f, localBoxFeather.value);
    }
}
