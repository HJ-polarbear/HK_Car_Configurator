namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreateArea")]
public class CreateAreaButton : EditorToolbarButton
{
    public CreateAreaButton()
    {
        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/URPLabStudio/Editor/Icons/area.png");

        tooltip = "Create Area Light (Baked Only)";
        clicked += () => CreateSimpleAreaLight("Area Light");
    }

    private void CreateSimpleAreaLight(string name)
    {
        GameObject lightGO = new GameObject(name);
        Light light = lightGO.AddComponent<Light>();

        light.type = LightType.Rectangle;
        light.lightmapBakeType = LightmapBakeType.Baked;

        lightGO.transform.position = new Vector3(0f, 2f, 0f);
        lightGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        light.color = Color.white;
        light.intensity = 2f;
        light.range = 5f;
        light.areaSize = new Vector2(1f, 1f);

        light.useColorTemperature = true;
        light.colorTemperature = 6500f;

        Undo.RegisterCreatedObjectUndo(lightGO, "Create Area Light");
        Selection.activeGameObject = lightGO;
    }
}
}
