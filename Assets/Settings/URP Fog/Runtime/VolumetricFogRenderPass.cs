using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

public sealed class VolumetricFogRenderPass : ScriptableRenderPass
{
    private class PassData
    {
        public Material material;
        public TextureHandle inputTexture;
        public int passIndex;
    }

    private class FogRenderData
    {
        public Material material;
        public TextureHandle depthTexture;
        public int frameCount;
        public float distance;
        public float baseHeight;
        public float maxHeight;
        public float density;
        public float absorption;
        public Color tint;
        public float mainAnisotropy;
        public float mainScattering;
        public int maxSteps;
        public bool useLocalBox;
        public Vector3 boxCenter;
        public Vector3 boxSize;
        public float boxFeather;
    }

    private static readonly int FrameCountId = Shader.PropertyToID("_FrameCount");
    private static readonly int DistanceId = Shader.PropertyToID("_Distance");
    private static readonly int BaseHeightId = Shader.PropertyToID("_BaseHeight");
    private static readonly int MaximumHeightId = Shader.PropertyToID("_MaximumHeight");
    private static readonly int DensityId = Shader.PropertyToID("_Density");
    private static readonly int AbsortionId = Shader.PropertyToID("_Absortion");
    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int MainLightAnisotropyId = Shader.PropertyToID("_MainLightAnisotropy");
    private static readonly int MainLightScatteringId = Shader.PropertyToID("_MainLightScattering");
    private static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
    private static readonly int HalfResDepthId = Shader.PropertyToID("_HalfResDepth");
    private static readonly int VolumetricFogTextureId = Shader.PropertyToID("_VolumetricFogTexture");
    private static readonly int UseLocalBoxId = Shader.PropertyToID("_UseLocalBox");
    private static readonly int LocalBoxCenterId = Shader.PropertyToID("_LocalBoxCenter");
    private static readonly int LocalBoxSizeId = Shader.PropertyToID("_LocalBoxSize");
    private static readonly int LocalBoxFeatherId = Shader.PropertyToID("_LocalBoxFeather");

    private readonly Material downsampleDepthMaterial;
    private readonly Material volumetricFogMaterial;

    public VolumetricFogRenderPass(Material downsampleDepthMaterial, Material volumetricFogMaterial)
    {
        this.downsampleDepthMaterial = downsampleDepthMaterial;
        this.volumetricFogMaterial = volumetricFogMaterial;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();
        var fogVolume = VolumeManager.instance.stack.GetComponent<VolumetricFogVolumeComponent>();

        if (fogVolume == null || !fogVolume.IsActive() || volumetricFogMaterial == null) return;

        RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
        desc.msaaSamples = 1;
        desc.depthBufferBits = 0;

        var halfResDesc = desc;
        halfResDesc.width = Mathf.Max(1, desc.width / 2);
        halfResDesc.height = Mathf.Max(1, desc.height / 2);
        halfResDesc.graphicsFormat = GraphicsFormat.R32_SFloat;
        TextureHandle halfResDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, halfResDesc, "_HalfResDepth", true);

        var fogDesc = halfResDesc;
        fogDesc.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
        TextureHandle fogTarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, fogDesc, "_VolumetricFog", true);

        // Pass 0: Depth Downsampling
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fog Downsample", out var passData))
        {
            passData.material = downsampleDepthMaterial;
            builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
            builder.SetRenderAttachment(halfResDepth, 0, AccessFlags.Write);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
                Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        // Pass 1: Volumetric Rendering
        using (var builder = renderGraph.AddRasterRenderPass<FogRenderData>("Render Fog", out var passData))
        {
            passData.material = volumetricFogMaterial;
            passData.depthTexture = halfResDepth;
            passData.frameCount = Time.renderedFrameCount % 64;
            passData.distance = fogVolume.distance.value;
            passData.baseHeight = fogVolume.baseHeight.value;
            passData.maxHeight = fogVolume.maximumHeight.value;
            passData.density = fogVolume.density.value;
            passData.absorption = 1.0f / Mathf.Max(0.0001f, fogVolume.attenuationDistance.value);
            passData.tint = fogVolume.tint.value;
            passData.mainAnisotropy = fogVolume.mainLightAnisotropy.value;
            passData.mainScattering = fogVolume.mainLightScattering.value;
            passData.maxSteps = fogVolume.maxSteps.value;
            passData.useLocalBox = fogVolume.useLocalBox.value;
            passData.boxCenter = fogVolume.localBoxCenter.value;
            passData.boxSize = fogVolume.localBoxSize.value;
            passData.boxFeather = fogVolume.localBoxFeather.value;

            builder.UseTexture(halfResDepth, AccessFlags.Read);
            builder.SetRenderAttachment(fogTarget, 0, AccessFlags.Write);
            builder.SetRenderFunc((FogRenderData data, RasterGraphContext context) => {
                data.material.SetInteger(FrameCountId, data.frameCount);
                data.material.SetFloat(DistanceId, data.distance);
                data.material.SetFloat(BaseHeightId, data.baseHeight);
                data.material.SetFloat(MaximumHeightId, data.maxHeight);
                data.material.SetFloat(DensityId, data.density);
                data.material.SetFloat(AbsortionId, data.absorption);
                data.material.SetColor(TintId, data.tint);
                data.material.SetFloat(MainLightAnisotropyId, data.mainAnisotropy);
                data.material.SetFloat(MainLightScatteringId, data.mainScattering);
                data.material.SetInteger(MaxStepsId, data.maxSteps);
                data.material.SetFloat(UseLocalBoxId, data.useLocalBox ? 1.0f : 0.0f);
                data.material.SetVector(LocalBoxCenterId, data.boxCenter);
                data.material.SetVector(LocalBoxSizeId, data.boxSize);
                data.material.SetFloat(LocalBoxFeatherId, data.boxFeather);
                data.material.SetTexture(HalfResDepthId, data.depthTexture);
                Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        // Pass 2: Composition
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Composite Fog", out var passData))
        {
            passData.material = volumetricFogMaterial;
            passData.inputTexture = fogTarget;
            passData.passIndex = 1; // Index of VolumetricFogComposition pass

            builder.UseTexture(fogTarget, AccessFlags.Read);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
                data.material.SetTexture(VolumetricFogTextureId, data.inputTexture);
                Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
            });
        }
    }

    public void Dispose() { }
}
