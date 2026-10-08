namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreateGrounded")]
public class CreateGroundedButton : EditorToolbarButton
{
    private const string kMatFolder = "Assets/URPLabStudio/Materials";
    private const string kMatPath = "Assets/URPLabStudio/Materials/Cube.mat";

    private const string kIconPath = "Assets/URPLabStudio/Editor/Icons/create.png";

    public CreateGroundedButton()
    {
        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath);
        tooltip = "Create Grounded Cube";
        clicked += CreateGroundedCube;
    }

    private static void CreateGroundedCube()
    {
        Transform parent = Selection.activeTransform;

        GameObject root = new GameObject("Cube_Root");
        Undo.RegisterCreatedObjectUndo(root, "Create Grounded Cube");

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

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(cube, "Create Cube");
        cube.transform.SetParent(root.transform, false);

        Renderer r = cube.GetComponent<Renderer>();
        if (r != null)
        {
            float yOffset = r.bounds.extents.y;
            cube.transform.localPosition = new Vector3(0f, yOffset, 0f);
            r.sharedMaterial = GetOrCreateCubeMaterial();
        }

        Selection.activeGameObject = root;
    }

    private static Material GetOrCreateCubeMaterial()
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

        ApplyCubeMaterialDefaults(mat);

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

    private static void ApplyCubeMaterialDefaults(Material mat)
    {
        if (mat == null)
            return;

        mat.color = Color.white;
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.5f);

        mat.SetColor("_EmissionColor", Color.black);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.EnableKeyword("_EMISSION");
    }
}
}
