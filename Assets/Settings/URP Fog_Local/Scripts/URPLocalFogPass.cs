using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class URPLocalFogPass : ScriptableRenderPass
{
    private static readonly int FogColorID = Shader.PropertyToID("_URPLocalFogColor");
    private static readonly int DensityID = Shader.PropertyToID("_URPLocalFogDensity");
    private static readonly int IntensityID = Shader.PropertyToID("_URPLocalFogIntensity");
    private static readonly int DistanceParamsID = Shader.PropertyToID("_URPLocalFogDistanceParams");
    private static readonly int HeightParamsID = Shader.PropertyToID("_URPLocalFogHeightParams");
    private static readonly int WorldToLocalID = Shader.PropertyToID("_URPLocalFogWorldToLocal");
    private static readonly int InverseViewProjectionID = Shader.PropertyToID("_URPLocalFogInvViewProj");

    private static readonly int NoiseToggleID = Shader.PropertyToID("_NoiseToggle");
    private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeedID = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int NoiseStrengthID = Shader.PropertyToID("_NoiseStrength");

    private readonly Material material;

    private class PassData
    {
        public Material material;
        public Matrix4x4 inverseViewProjectionMatrix;
    }

    public URPLocalFogPass(Material material)
    {
        this.material = material;
        ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (material == null)
        {
            return;
        }

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (resourceData.isActiveTargetBackBuffer)
        {
            return;
        }

        Camera camera = cameraData.camera;
        if (camera == null)
        {
            return;
        }

        TextureHandle colorTarget = resourceData.activeColorTexture;
        TextureHandle depthTarget = resourceData.cameraDepthTexture;

        using (IRasterRenderGraphBuilder builder =
               renderGraph.AddRasterRenderPass<PassData>("URP Local Fog Pass", out PassData passData))
        {
            passData.material = material;
            passData.inverseViewProjectionMatrix = GetInverseViewProjectionMatrix(camera);

            builder.UseTexture(depthTarget, AccessFlags.Read);
            builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                data.material.SetMatrix(InverseViewProjectionID, data.inverseViewProjectionMatrix);

                for (int i = 0; i < URPLocalFogVolume.ActiveVolumes.Count; i++)
                {
                    URPLocalFogVolume volume = URPLocalFogVolume.ActiveVolumes[i];

                    if (volume == null || !volume.isActiveAndEnabled)
                    {
                        continue;
                    }

                    data.material.SetColor(FogColorID, volume.fogColor);
                    data.material.SetFloat(DensityID, Mathf.Max(0.0f, volume.density));
                    data.material.SetFloat(IntensityID, Mathf.Clamp01(volume.intensity));
                    data.material.SetMatrix(WorldToLocalID, volume.WorldToLocalMatrix);

                    data.material.SetFloat(NoiseToggleID, volume.useNoise ? 1.0f : 0.0f);
                    data.material.SetFloat(NoiseScaleID, Mathf.Max(0.1f, volume.noiseScale));
                    data.material.SetFloat(NoiseSpeedID, Mathf.Max(0.0f, volume.noiseSpeed));
                    data.material.SetFloat(NoiseStrengthID, Mathf.Clamp01(volume.noiseStrength));

                    float startDistance = Mathf.Max(0.0f, volume.startDistance);
                    float endDistance = Mathf.Max(startDistance + 0.001f, volume.endDistance);

                    data.material.SetVector(
                        DistanceParamsID,
                        new Vector4(startDistance, endDistance, 1.0f / (endDistance - startDistance), 0.0f)
                    );

                    float heightStart = volume.heightStart;
                    float heightEnd = Mathf.Max(heightStart + 0.001f, volume.heightEnd);

                    data.material.SetVector(
                        HeightParamsID,
                        new Vector4(
                            volume.useHeightFade ? 1.0f : 0.0f,
                            heightStart,
                            heightEnd,
                            1.0f / (heightEnd - heightStart)
                        )
                    );

                    CoreUtils.DrawFullScreen(context.cmd, data.material);
                }
            });
        }
    }

    private static Matrix4x4 GetInverseViewProjectionMatrix(Camera camera)
    {
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        Matrix4x4 viewProjectionMatrix = projectionMatrix * viewMatrix;

        return viewProjectionMatrix.inverse;
    }
}