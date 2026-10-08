namespace URPLabStudio
{
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[EditorToolbarElement("URPLab/URPSet")]
public sealed class Lab_SetButton : EditorToolbarButton
{
    private const string RootFolder = "Assets/URPLabStudio";
    private const string SettingsFolder = RootFolder + "/Settings";
    private const string QualityFolder = SettingsFolder + "/URP_Quality";
    private const string ProfileFolder = SettingsFolder + "/URP_Profile";

    public Lab_SetButton()
    {
        text = "URPSet";
        tooltip = "Organize URP assets and relink the project URP pipeline settings.";
        icon = EditorGUIUtility.IconContent("d_SceneViewFx").image as Texture2D;
        clicked += ConfigureHdrpProject;
    }

    private static void ConfigureHdrpProject()
    {
        try
        {
            EnsureFolders();
            MoveAssetsByType<UniversalRenderPipelineAsset>(QualityFolder);
            MoveAssetsByType<VolumeProfile>(ProfileFolder);

            List<UniversalRenderPipelineAsset> pipelines = FindAssets<UniversalRenderPipelineAsset>(QualityFolder);
            if (pipelines.Count == 0)
            {
                EditorUtility.DisplayDialog("URPSet", "URP Render Pipeline Asset을 찾지 못했습니다.", "OK");
                return;
            }

            UniversalRenderPipelineAsset pipeline = ChooseDefaultPipeline(pipelines);
            GraphicsSettings.defaultRenderPipeline = pipeline;

            int originalQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(originalQuality, false);

            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"URPSet completed: {AssetDatabase.GetAssetPath(pipeline)}");
            EditorUtility.DisplayDialog("URPSet", "URP 설정 정리 및 파이프라인 연결을 완료했습니다.", "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("URPSet", "URPSet 실행에 실패했습니다. Console을 확인하세요.", "OK");
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "URPLabStudio");
        EnsureFolder(RootFolder, "Settings");
        EnsureFolder(SettingsFolder, "URP_Quality");
        EnsureFolder(SettingsFolder, "URP_Profile");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static void MoveAssetsByType<T>(string destinationFolder) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { SettingsFolder }))
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
            if (sourcePath.StartsWith(destinationFolder + "/", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationFolder + "/" + Path.GetFileName(sourcePath));
            string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning($"URPSet move failed: {sourcePath}\n{error}");
        }
    }

    private static List<T> FindAssets<T>(string folder) where T : Object
    {
        var assets = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) assets.Add(asset);
        }
        return assets;
    }

    private static UniversalRenderPipelineAsset ChooseDefaultPipeline(List<UniversalRenderPipelineAsset> assets)
    {
        RenderPipelineAsset current = GraphicsSettings.defaultRenderPipeline;
        foreach (UniversalRenderPipelineAsset asset in assets)
            if (asset == current) return asset;

        assets.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        return assets[0];
    }
}
#endif
}
