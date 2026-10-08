namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Lab_LightUtils
{
    private const string kRootFolder = "Assets/URPLabStudio";
    private const string kMatFolder = "Assets/URPLabStudio/Materials";
    private const string kEmissiveMatPath = "Assets/URPLabStudio/Materials/EmissiveWhite_HDRP.mat";

    public static void CreateLight(LightType type, string name)
    {
        // URP does not provide true realtime rectangle area light.
        // Treat Rectangle as an emissive + spot simulation.
        if (type == LightType.Rectangle)
        {
            CreateEmissiveAreaLight(name);
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        GameObject lightGO = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(lightGO, $"Create {type} Light");

        Light light = Undo.AddComponent<Light>(lightGO);
        light.type = type;
        light.lightmapBakeType = LightmapBakeType.Mixed;

        ApplyDefaults(light);
        PlaceObjectInScene(lightGO, type, parentToSelection: true);

        Selection.activeGameObject = lightGO;
        MarkSceneDirty();

        Undo.CollapseUndoOperations(group);
    }

    private static void ApplyDefaults(Light light)
    {
        light.color = Color.white;
        light.useColorTemperature = true;
        light.colorTemperature = 6500f;

        switch (light.type)
        {
            case LightType.Directional:
                light.intensity = 1.0f;
                light.shadows = LightShadows.Soft;
                break;

            case LightType.Point:
                light.intensity = 1.0f;
                light.range = 10f;
                light.shadows = LightShadows.Soft;
                break;

            case LightType.Spot:
                light.intensity = 1.0f;
                light.range = 15f;
                light.spotAngle = 30f;
                light.shadows = LightShadows.Soft;
                break;

            default:
                Debug.LogWarning($"Light type '{light.type}' may not be supported in the current render pipeline.");
                break;
        }
    }

    private static void CreateEmissiveAreaLight(string name)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        GameObject root = new GameObject(name + "_EmissiveArea");
        Undo.RegisterCreatedObjectUndo(root, "Create Emissive Area Light");

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(plane, "Create Emissive Quad");
        Undo.SetTransformParent(plane.transform, root.transform, "Parent Emissive Quad");

        plane.name = "EmissiveQuad";
        plane.transform.localPosition = Vector3.zero;
        plane.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        plane.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        Collider col = plane.GetComponent<Collider>();
        if (col != null)
        {
            Undo.DestroyObjectImmediate(col);
        }

        Material mat = GetOrCreateHdrpEmissiveMaterial();
        Renderer renderer = plane.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = mat;
        }

        GameObject spotGO = new GameObject("SpotLight");
        Undo.RegisterCreatedObjectUndo(spotGO, "Create Spot Light");
        Undo.SetTransformParent(spotGO.transform, root.transform, "Parent Spot Light");

        spotGO.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        spotGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spotGO.transform.localScale = Vector3.one;

        Light spot = Undo.AddComponent<Light>(spotGO);
        spot.type = LightType.Spot;
        spot.spotAngle = 90f;
        spot.range = 6f;
        spot.intensity = 2f;
        spot.color = Color.white;
        spot.useColorTemperature = true;
        spot.colorTemperature = 6500f;
        spot.shadows = LightShadows.Soft;
        spot.lightmapBakeType = LightmapBakeType.Mixed;

        PlaceObjectInScene(root, LightType.Spot, parentToSelection: true);

        Selection.activeGameObject = root;
        MarkSceneDirty();

        Undo.CollapseUndoOperations(group);
    }

    private static Material GetOrCreateHdrpEmissiveMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(kEmissiveMatPath);
        if (mat != null)
        {
            return mat;
        }

        EnsureHdrpLabFoldersExist();

        Shader hdrpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (hdrpLit == null)
        {
            Debug.LogWarning("URP Lit shader not found. Falling back to Standard.");
            hdrpLit = Shader.Find("Standard");
        }

        if (hdrpLit == null)
        {
            Debug.LogError("Could not find a valid shader for emissive material creation.");
            return null;
        }

        mat = new Material(hdrpLit);

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.white);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", Color.white);
        }

        mat.EnableKeyword("_EMISSION");

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", Color.white * 2f);
        }

        AssetDatabase.CreateAsset(mat, kEmissiveMatPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return mat;
    }

    private static void EnsureHdrpLabFoldersExist()
    {
        if (!AssetDatabase.IsValidFolder(kRootFolder))
        {
            AssetDatabase.CreateFolder("Assets", "URPLabStudio");
        }

        if (!AssetDatabase.IsValidFolder(kMatFolder))
        {
            AssetDatabase.CreateFolder(kRootFolder, "Materials");
        }
    }

    private static void PlaceObjectInScene(GameObject go, LightType type, bool parentToSelection)
    {
        Transform parent = parentToSelection ? Selection.activeTransform : null;

        if (parent != null)
        {
            Undo.SetTransformParent(go.transform, parent, "Set Parent");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return;
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.camera != null)
        {
            Transform cam = sceneView.camera.transform;
            go.transform.position = cam.position + cam.forward * 5f;

            if (type == LightType.Directional || type == LightType.Spot)
            {
                go.transform.rotation = Quaternion.LookRotation(cam.forward, Vector3.up);
            }
            else
            {
                go.transform.rotation = Quaternion.identity;
            }

            go.transform.localScale = Vector3.one;
            return;
        }

        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
    }

    private static void MarkSceneDirty()
    {
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
}
