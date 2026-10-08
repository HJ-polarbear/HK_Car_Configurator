namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_CreatePrimitiveDropdown")]
public class Lab_CreatePrimitiveDropdown : EditorToolbarDropdown
{
    private static readonly PrimitiveType[] s_Types =
    {
        PrimitiveType.Cube,
        PrimitiveType.Sphere,
        PrimitiveType.Capsule,
        PrimitiveType.Cylinder,
        PrimitiveType.Plane
    };

    private static readonly string[] s_TypeNames =
    {
        "Cube",
        "Sphere",
        "Capsule",
        "Cylinder",
        "Plane"
    };

    private const string kIconPath = "Assets/URPLabStudio/Editor/Icons/create.png";
    private const string kMatFolder = "Assets/URPLabStudio/Materials";
    private const string kPrefsKey = "URPLab.Lab_CreatePrimitiveDropdown.SelectedType";

    private PrimitiveType SelectedType
    {
        get => (PrimitiveType)EditorPrefs.GetInt(kPrefsKey, (int)PrimitiveType.Cube);
        set => EditorPrefs.SetInt(kPrefsKey, (int)value);
    }

    public Lab_CreatePrimitiveDropdown()
    {
        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath);
        tooltip = "Create Grounded Primitive";
        clicked += ShowDropdownMenu;
    }

    private void ShowDropdownMenu()
    {
        PrimitiveType current = SelectedType;

        GenericMenu menu = new GenericMenu();
        for (int i = 0; i < s_Types.Length; i++)
        {
            PrimitiveType type = s_Types[i];
            string label = s_TypeNames[i];

            menu.AddItem(new GUIContent(label), current == type, () =>
            {
                SelectedType = type;
                CreateGroundedPrimitive(type);
            });
        }

        menu.ShowAsContext();
    }

    private static void CreateGroundedPrimitive(PrimitiveType type)
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

        GameObject obj = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(obj, $"Create {type}");
        obj.transform.SetParent(root.transform, false);

        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            float yOffset = r.bounds.extents.y;
            obj.transform.localPosition = new Vector3(0f, yOffset, 0f);
            r.sharedMaterial = GetOrCreateMaterial(type);
        }

        Selection.activeGameObject = root;
    }

    private static Material GetOrCreateMaterial(PrimitiveType type)
    {
        string matPath = $"{kMatFolder}/{type}.mat";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            EnsureFolderExists("Assets/URPLabStudio");
            EnsureFolderExists(kMatFolder);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        ApplyDefaults(mat);

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

    private static void ApplyDefaults(Material mat)
    {
        if (mat == null)
            return;

        mat.color = new Color(0.8f, 0.9f, 1.0f);
        mat.SetFloat("_Smoothness", 0.5f);
        mat.SetFloat("_Metallic", 0.1f);
    }
}
}
