namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreateSphere")]
public class CreateSphereButton : EditorToolbarButton
{
    private const string kMatFolder = "Assets/URPLabStudio/Materials";
    private const string kMatPath = "Assets/URPLabStudio/Materials/Sphere.mat";

    public CreateSphereButton()
    {
        icon = EditorGUIUtility.IconContent("d_SphereCollider Icon").image as Texture2D;
        tooltip = "Create Sphere at Ground";
        clicked += OnClicked;
    }

    private void OnClicked()
    {
        CreatePrimitiveAtGround(PrimitiveType.Sphere, removeCollider: false);
    }

    private static void CreatePrimitiveAtGround(PrimitiveType type, bool removeCollider)
    {
        Transform parent = Selection.activeTransform;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        GameObject root = new GameObject($"{type}_Root");
        Undo.RegisterCreatedObjectUndo(root, $"Create {type} Root");

        if (parent != null)
        {
            Undo.SetTransformParent(root.transform, parent, "Set Parent");
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
        }
        else
        {
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
        }

        GameObject mesh = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(mesh, $"Create {type}");

        Undo.SetTransformParent(mesh.transform, root.transform, "Parent Mesh");
        mesh.transform.localRotation = Quaternion.identity;
        mesh.transform.localScale = Vector3.one;

        if (removeCollider)
        {
            Collider col = mesh.GetComponent<Collider>();
            if (col != null)
                Undo.DestroyObjectImmediate(col);
        }

        float yOffset = CalculateYOffset(mesh);
        mesh.transform.localPosition = new Vector3(0f, yOffset, 0f);

        ApplyUrpLitMaterial(mesh);

        Selection.activeGameObject = root;

        Undo.CollapseUndoOperations(group);
    }

    private static float CalculateYOffset(GameObject go)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
                return r.bounds.extents.y;

            return 0.5f;
        }

        Bounds b = mf.sharedMesh.bounds;
        float extY = b.extents.y;
        float scaleY = Mathf.Abs(go.transform.localScale.y);
        return extY * scaleY;
    }

    private static void ApplyUrpLitMaterial(GameObject go)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(kMatPath);
        bool created = false;

        if (mat == null)
        {
            EnsureFolderExists("Assets/URPLabStudio");
            EnsureFolderExists(kMatFolder);

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogWarning("URP Lit shader not found. Using Standard shader as fallback.");
                lit = Shader.Find("Standard");
            }

            mat = new Material(lit);
            created = true;
            AssetDatabase.CreateAsset(mat, kMatPath);
        }

        mat.color = new Color(0.9f, 0.75f, 0.2f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.85f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.4f);

        EditorUtility.SetDirty(mat);
        if (created)
            AssetDatabase.SaveAssets();

        renderer.sharedMaterial = mat;
    }

    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);

        EnsureFolderExists(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
}
