namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreateEmpty")]
public class CreateEmptyButton : EditorToolbarButton
{
    private const string kIconPath = "Assets/URPLabStudio/Editor/Icons/empty.png";

    public CreateEmptyButton()
    {
        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath);
        tooltip = "Create Empty GameObject";
        clicked += Lab_CreateEmpty;
    }

    private static void Lab_CreateEmpty()
    {
        Transform parent = Selection.activeTransform;

        string baseName = "EmptyObject";
        string finalName = GetUniqueName(baseName);

        GameObject empty = new GameObject(finalName);
        Undo.RegisterCreatedObjectUndo(empty, "Create Empty GameObject");

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
        {
            Transform cam = SceneView.lastActiveSceneView.camera.transform;
            spawnPosition = cam.position + cam.forward * 5f;
            spawnRotation = Quaternion.LookRotation(-cam.forward);
        }

        if (parent != null)
        {
            empty.transform.SetParent(parent, false);
            empty.transform.localPosition = Vector3.zero;
            empty.transform.localRotation = Quaternion.identity;
            empty.transform.localScale = Vector3.one;
        }
        else
        {
            empty.transform.position = spawnPosition;
            empty.transform.rotation = spawnRotation;
        }

        empty.tag = "Untagged";
        empty.layer = 0;

        Selection.activeGameObject = empty;
    }

    private static string GetUniqueName(string baseName)
    {
        string finalName = baseName;
        int index = 1;

        while (GameObjectNameExists(finalName))
        {
            finalName = $"{baseName}_{index++}";
        }

        return finalName;
    }

    private static bool GameObjectNameExists(string name)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name)
            {
                return true;
            }
        }

        return false;
    }
}
}
