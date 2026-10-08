namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreateCapsule")]
public class CreateCapsuleButton : EditorToolbarButton
{
    private const string kMatFolder = "Assets/URPLabStudio/Materials";
    private const string kMatPath = "Assets/URPLabStudio/Materials/Capsule.mat";

    public CreateCapsuleButton()
    {
        icon = EditorGUIUtility.IconContent("d_CapsuleCollider Icon").image as Texture2D;
        tooltip = "Create Capsule at Ground";
        clicked += () => CreatePrimitiveAtGround(PrimitiveType.Capsule);
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
            r.sharedMaterial = GetOrCreateCapsuleMaterial();
        }

        Selection.activeGameObject = root;
    }

    private static Material GetOrCreateCapsuleMaterial()
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

        ApplyCapsuleMaterialDefaults(mat);

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

    private static void ApplyCapsuleMaterialDefaults(Material mat)
    {
        if (mat == null)
            return;

        mat.color = new Color(0.5f, 0.9f, 0.6f);
        mat.SetFloat("_Smoothness", 0.7f);
        mat.SetFloat("_Metallic", 0.3f);
    }
}
}
