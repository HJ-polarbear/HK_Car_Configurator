namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreateDirectional")]
public class CreateDirectionalButton : EditorToolbarButton
{
    private const string kIconPath = "Assets/URPLabStudio/Editor/Icons/dir.png";

    public CreateDirectionalButton()
    {
        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath);
        tooltip = "Create Directional Light";
        clicked += () => CreateDirectionalLight("Directional Light");
    }

    private static void CreateDirectionalLight(string name)
    {
        GameObject lightGO = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(lightGO, "Create Directional Light");

        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;

        light.intensity = 1f;
        light.useColorTemperature = true;
        light.colorTemperature = 6500f;
        light.color = Color.white;

        light.shadows = LightShadows.Soft;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.4f;
        light.renderMode = LightRenderMode.Auto;

        light.lightmapBakeType = LightmapBakeType.Mixed;

        lightGO.transform.position = new Vector3(0f, 3f, 0f);
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Selection.activeGameObject = lightGO;
    }
}
}
