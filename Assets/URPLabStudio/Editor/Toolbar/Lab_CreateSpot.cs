namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreateSpot")]
public class CreateSpotButton : EditorToolbarButton
{
    private const string kIconPath = "Assets/URPLabStudio/Editor/Icons/spot.png";
    private const string kFallbackIconName = "d_Light Icon";

    public CreateSpotButton()
    {
        icon = LoadIcon();
        tooltip = "Create Spot Light";
        clicked += OnClicked;
    }

    private static Texture2D LoadIcon()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath);
        if (tex != null)
        {
            return tex;
        }

        return EditorGUIUtility.IconContent(kFallbackIconName).image as Texture2D;
    }

    private void OnClicked()
    {
        CreateSpotLight("Spot Light", true);
    }

    private static void CreateSpotLight(string objectName, bool parentToSelection)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        Transform parent = parentToSelection ? Selection.activeTransform : null;

        GameObject go = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(go, "Create Spot Light");

        Transform tr = go.transform;

        if (parent != null)
        {
            Undo.SetTransformParent(tr, parent, "Set Parent");
            tr.localPosition = new Vector3(0f, 2f, 0f);
            tr.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tr.localScale = Vector3.one;
        }
        else
        {
            tr.position = new Vector3(0f, 2f, 0f);
            tr.rotation = Quaternion.Euler(90f, 0f, 0f);
            tr.localScale = Vector3.one;
        }

        Light lightComp = Undo.AddComponent<Light>(go);
        lightComp.type = LightType.Spot;

        lightComp.intensity = 1f;
        lightComp.range = 15f;
        lightComp.spotAngle = 30f;
        lightComp.shadows = LightShadows.Soft;

        lightComp.useColorTemperature = true;
        lightComp.colorTemperature = 6500f;
        lightComp.color = Color.white;

        Selection.activeGameObject = go;

        Undo.CollapseUndoOperations(group);
    }
}
}
