namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreatePlane")]
public class CreatePlaneButton : EditorToolbarButton
{
    private const string kMatFolder = "Assets/URPLabStudio/Materials";
    private const string kMatPath = "Assets/URPLabStudio/Materials/Plane.mat";

    public CreatePlaneButton()
    {
        icon = EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D;
        tooltip = "Create Plane at Ground";
        clicked += () => CreatePrimitiveAtGround(PrimitiveType.Plane);
    }

    private static void CreatePrimitiveAtGround(PrimitiveType type)
    {
        Transform parent = Selection.activeTransform;

        GameObject root = new GameObject($"{type}_Root");
        Undo.RegisterCreatedObjectUndo(root, $"Create Grounded {type}");

        if (parent != null)
        {
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
        }
        else
        {
            root.transform.position = Vector3.zero;
        }

        GameObject mesh = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(mesh, $"Create {type}");
        mesh.transform.SetParent(root.transform, false);

        Renderer r = mesh.GetComponent<Renderer>();
        if (r != null)
        {
            float yOffset = r.bounds.extents.y;
            mesh.transform.localPosition = new Vector3(0f, yOffset, 0f);
            r.sharedMaterial = GetOrCreatePlaneMaterial();
        }

        Selection.activeGameObject = root;
    }

    private static Material GetOrCreatePlaneMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(kMatPath);

        if (mat == null)
        {
            EnsureFolderExists("Assets/URPLabStudio");
            EnsureFolderExists(kMatFolder);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, kMatPath);
        }

        ApplyPlaneMaterialDefaults(mat);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        return mat;
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

    private static void ApplyPlaneMaterialDefaults(Material mat)
    {
        if (mat == null)
            return;

        mat.color = new Color(0.85f, 0.85f, 0.85f);
        mat.SetFloat("_Smoothness", 0.4f);
        mat.SetFloat("_Metallic", 0.0f);
    }
}
}
