namespace URPLabStudio
{
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Lab_DecalPresetSettings",
    menuName = "URP Lab/Decal Preset Settings")]
public sealed class Lab_DecalPresetSettings : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/URPLabStudio/Adapters/URP/Editor/Decal/Lab_DecalPresetSettings.asset";

    private static readonly string[] DefaultShaderNames =
    {
        "UVC_DecalAlphaEm",
        "UVC_DecalBlackEm",
        "UVC_DecalGrime",
        "UVC_DecalLightFlickering",
        "UVC_DecalReflections",
        "UVC_DecalSignsEm"
    };

    [Header("URPLab Decal Presets")]
    [Tooltip("Shaders shown in the Scene view URPLab Decal Presets dropdown.")]
    public Shader[] presetShaders = new Shader[6];

    public static Lab_DecalPresetSettings LoadOrCreate()
    {
        Lab_DecalPresetSettings settings =
            AssetDatabase.LoadAssetAtPath<Lab_DecalPresetSettings>(DefaultAssetPath);

        if (settings != null)
            return settings;

        settings = CreateInstance<Lab_DecalPresetSettings>();
        settings.presetShaders = new Shader[DefaultShaderNames.Length];

        for (int i = 0; i < DefaultShaderNames.Length; i++)
            settings.presetShaders[i] = FindShaderAsset(DefaultShaderNames[i]);

        AssetDatabase.CreateAsset(settings, DefaultAssetPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static Shader FindShaderAsset(string shaderName)
    {
        string[] guids = AssetDatabase.FindAssets($"{shaderName} t:Shader");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != shaderName)
                continue;

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null)
                return shader;
        }

        return Shader.Find($"UVC Shader/{shaderName}");
    }
}

[InitializeOnLoad]
internal static class URPDecalPresetSettingsInitializer
{
    static URPDecalPresetSettingsInitializer()
    {
        EditorApplication.delayCall += EnsureSettingsAsset;
    }

    private static void EnsureSettingsAsset()
    {
        Lab_DecalPresetSettings.LoadOrCreate();
    }
}
#endif
}
