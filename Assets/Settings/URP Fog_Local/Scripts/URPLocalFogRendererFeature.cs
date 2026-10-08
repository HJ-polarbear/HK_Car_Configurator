using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPLocalFogRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Shader localFogShader;
    }

    public Settings settings = new Settings();

    private URPLocalFogPass localFogPass;
    private Material localFogMaterial;

    public override void Create()
    {
        if (settings.localFogShader == null)
        {
            settings.localFogShader = Shader.Find("Hidden/URPLab/URPLocalFog");
        }

        if (settings.localFogShader != null)
        {
            localFogMaterial = CoreUtils.CreateEngineMaterial(settings.localFogShader);
        }

        localFogPass = new URPLocalFogPass(localFogMaterial)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (localFogMaterial == null)
        {
            return;
        }

        if (URPLocalFogVolume.ActiveVolumes.Count == 0)
        {
            return;
        }

        Camera camera = renderingData.cameraData.camera;
        if (camera == null)
        {
            return;
        }

        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
        {
            return;
        }

        renderer.EnqueuePass(localFogPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(localFogMaterial);
    }
}