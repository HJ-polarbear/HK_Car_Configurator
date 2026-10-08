namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreatePoint")]
public class CreatePointButton : EditorToolbarButton
{
    private const string kIconPath = "Assets/URPLabStudio/Editor/Icons/point.png";

    public CreatePointButton()
    {
        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath);
        tooltip = "Create Point Light";
        clicked += () => CreatePointLight("Point Light");
    }

    private static void CreatePointLight(string name)
    {
        Transform parent = Selection.activeTransform;

        GameObject lightGO = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(lightGO, "Create Point Light");

        if (parent != null)
        {
            lightGO.transform.SetParent(parent, false);
            lightGO.transform.localPosition = Vector3.zero;
        }
        else
        {
            lightGO.transform.position = Vector3.zero;
        }

        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;

        light.intensity = 1f;
        light.range = 10f;
        light.shadows = LightShadows.Soft;

        light.useColorTemperature = true;
        light.colorTemperature = 6500f;
        light.color = Color.white;

        Selection.activeGameObject = lightGO;
    }
}
}
