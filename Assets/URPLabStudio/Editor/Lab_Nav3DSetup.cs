namespace URPLabStudio.Editor
{
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Lab_Nav3DSetup
{
    const string ExitPrefabPath = "Assets/URPLabStudio/Textures/UI/3D Icons/Prefabs/3D_Icon_Logout.prefab";
    const string BackPrefabPath = "Assets/URPLabStudio/Textures/UI/3D Icons/Prefabs/3D_Icon_Back.prefab";

    [MenuItem("Tools/URPLab Studio/Configure/3D Navigation")]
    public static void Configure()
    {
        Lab_InteriorCameraController controller = Object.FindAnyObjectByType<Lab_InteriorCameraController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.LogError("Lab_InteriorCameraController was not found in the open scene.");
            return;
        }

        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
        GameObject backPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackPrefabPath);
        if (exitPrefab == null || backPrefab == null)
        {
            Debug.LogError("Lab_Nav3D icon prefabs were not found.");
            return;
        }

        Transform fixedRoot = controller.transform.root.Find("URP_HybridB_UI");
        if (fixedRoot == null)
            fixedRoot = controller.transform.root;

        Transform existing = controller.transform.root.Find("Lab_Nav3D");
        GameObject navigationObject;
        if (existing != null)
        {
            navigationObject = existing.gameObject;
        }
        else
        {
            navigationObject = new GameObject("Lab_Nav3D");
            Undo.RegisterCreatedObjectUndo(navigationObject, "Create Lab Nav 3D");
            navigationObject.transform.SetParent(fixedRoot, false);
        }

        navigationObject.transform.SetParent(fixedRoot, false);

        Lab_Nav3D navigation = navigationObject.GetComponent<Lab_Nav3D>();
        if (navigation == null)
            navigation = Undo.AddComponent<Lab_Nav3D>(navigationObject);

        Undo.RecordObject(navigation, "Configure Lab Nav 3D");
        navigation.Configure(controller, exitPrefab, backPrefab);
        Undo.RecordObject(controller, "Assign Lab Nav 3D");
        controller.SetNavigation3D(navigation);

        RemoveSceneIcon("3D_Icon_Logout");
        RemoveSceneIcon("3D_Icon_Back");

        EditorUtility.SetDirty(navigation);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = navigationObject;
        Debug.Log("Lab_Nav3D configured. Navigation icons will be created at runtime.");
    }

    static void RemoveSceneIcon(string objectName)
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.name != objectName || !candidate.scene.IsValid())
                continue;
            Undo.DestroyObjectImmediate(candidate);
        }
    }
}
}
