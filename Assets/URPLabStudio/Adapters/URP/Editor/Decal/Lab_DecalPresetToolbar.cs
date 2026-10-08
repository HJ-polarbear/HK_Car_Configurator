namespace URPLabStudio
{
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Overlay(typeof(SceneView), "URPLab Decal Presets", true)]
public class Lab_DecalPresetToolbar : ToolbarOverlay
{
    public Lab_DecalPresetToolbar() : base(
        DecalPresetDropdown.ID,
        DecalCreateButton.ID,
        DecalBrushToggle.ID)
    {
    }
}

[EditorToolbarElement(ID)]
public class DecalPresetDropdown : EditorToolbarDropdown
{
    public const string ID = "URPLab/DecalPresetDropdown";

    private sealed class StaticState { public int SelectedIndex; }
    private static readonly StaticState State = new StaticState();
    public static int SelectedIndex => State.SelectedIndex;

    public static Shader SelectedShader
    {
        get
        {
            Shader[] presets = GetPresetShaders();
            if (presets.Length == 0)
                return null;

            State.SelectedIndex = Mathf.Clamp(State.SelectedIndex, 0, presets.Length - 1);
            return presets[SelectedIndex];
        }
    }

    public DecalPresetDropdown()
    {
        UpdateText();

        clicked += () =>
        {
            GenericMenu menu = new GenericMenu();

            Shader[] presets = GetPresetShaders();
            for (int i = 0; i < presets.Length; i++)
            {
                int index = i;
                Shader shader = presets[i];
                string label = shader != null ? GetDisplayName(shader) : $"Empty Slot {i + 1}";

                if (shader == null)
                {
                    menu.AddDisabledItem(new GUIContent(label));
                    continue;
                }

                menu.AddItem(new GUIContent(label), SelectedIndex == i, () =>
                {
                    State.SelectedIndex = index;
                    UpdateText();
                });
            }

            menu.DropDown(worldBound);
        };
    }

    private void UpdateText()
    {
        Shader shader = SelectedShader;
        text = shader != null ? GetDisplayName(shader) : "No Decal Preset";
    }

    private static Shader[] GetPresetShaders()
    {
        Lab_DecalPresetSettings settings = Lab_DecalPresetSettings.LoadOrCreate();
        return settings != null && settings.presetShaders != null
            ? settings.presetShaders
            : System.Array.Empty<Shader>();
    }

    private static string GetDisplayName(Shader shader)
    {
        int separator = shader.name.LastIndexOf('/');
        return separator >= 0 ? shader.name.Substring(separator + 1) : shader.name;
    }
}

[EditorToolbarElement(ID)]
public class DecalCreateButton : EditorToolbarButton
{
    public const string ID = "URPLab/DecalCreateButton";

    public DecalCreateButton()
    {
        icon = EditorGUIUtility.IconContent("d_PreMatCube").image as Texture2D;
        text = "Create";
        tooltip = "Create a decal object using the selected preset.";
        clicked += () => DecalBrushTool.CreateSingleDecalFromCurrentPreset();
    }
}

[EditorToolbarElement(ID)]
public class DecalBrushToggle : EditorToolbarButton
{
    public const string ID = "URPLab/DecalBrushToggle";

    public DecalBrushToggle()
    {
        icon = EditorGUIUtility.IconContent("d_TerrainInspector.TerrainToolPlants").image as Texture2D;
        UpdateVisualState();

        clicked += () =>
        {
            DecalBrushTool.ToggleBrushEnabled();
            UpdateVisualState();
            SceneView.RepaintAll();
        };

        EditorApplication.update += UpdateVisualState;
    }

    private void UpdateVisualState()
    {
        text = DecalBrushTool.IsBrushEnabled ? "Brush ON" : "Brush OFF";
        tooltip = DecalBrushTool.IsBrushEnabled
            ? "Decal brush is enabled."
            : "Decal brush is disabled.";
    }
}

[InitializeOnLoad]
public static class DecalBrushTool
{
    private const string RootFolder = "Assets/URPLabStudio";
    private const string MaterialsFolder = "Assets/URPLabStudio/Materials";
    private const string DecalsFolder = "Assets/URPLabStudio/Materials/Decals";

    private const float SurfaceOffset = 0.01f;
    private const float BrushSpacing = 0.5f;
    private const float CameraPlacementDistance = 3f;

    private static readonly Vector3 DefaultProjectorSize = new Vector3(2f, 1f, 1f);
    private static readonly Vector3 DefaultProjectorPivot = Vector3.zero;
    private static readonly Vector3 SingleCreateEuler = new Vector3(90f, 0f, 0f);

    private sealed class StaticState
    {
        public bool IsBrushEnabled;
        public Vector3 LastBrushPosition;
        public bool HasLastBrushPosition;
    }
    private static readonly StaticState State = new StaticState();

    public static bool IsBrushEnabled => State.IsBrushEnabled;

    public static void ToggleBrushEnabled()
    {
        State.IsBrushEnabled = !State.IsBrushEnabled;
        State.HasLastBrushPosition = false;
    }

    static DecalBrushTool()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;

        EditorApplication.update -= SyncSelectedDecalScale;
        EditorApplication.update += SyncSelectedDecalScale;
    }

    public static void CreateSingleDecalFromCurrentPreset()
    {
        Shader shader = DecalPresetDropdown.SelectedShader;
        if (shader == null) return;

        string shaderKey = GetShaderKey(shader);

        Material material = GetOrCreateDecalMaterial(shaderKey, shader);
        if (material == null) return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        GameObject decalGO = CreateDecalGameObject(shaderKey, material);
        PlaceDecalForSingleCreate(decalGO);

        Selection.activeGameObject = decalGO;

        Undo.CollapseUndoOperations(group);
    }

    private static GameObject CreateDecalGameObject(string shaderKey, Material material)
    {
        GameObject decalGO = new GameObject($"Decal_{shaderKey}");
        Undo.RegisterCreatedObjectUndo(decalGO, "Create Decal");

        DecalProjector projector = Undo.AddComponent<DecalProjector>(decalGO);
        projector.material = material;

        ApplyDefaultProjectorSettings(projector);

        decalGO.transform.localScale = Vector3.one;

        return decalGO;
    }

    private static void ApplyDefaultProjectorSettings(DecalProjector projector)
    {
        projector.size = DefaultProjectorSize;
        projector.pivot = DefaultProjectorPivot;
        projector.drawDistance = 100f;
        projector.fadeFactor = 0.9f;
    }

    private static void PlaceDecalForSingleCreate(GameObject decalGO)
    {
        SceneView view = SceneView.lastActiveSceneView;

        if (view != null && view.camera != null)
        {
            Transform cam = view.camera.transform;
            decalGO.transform.position = cam.position + cam.forward * CameraPlacementDistance;
        }

        decalGO.transform.rotation = Quaternion.Euler(SingleCreateEuler);
    }

    private static void CreateBrushDecal(RaycastHit hit)
    {
        Shader shader = DecalPresetDropdown.SelectedShader;
        if (shader == null) return;

        string shaderKey = GetShaderKey(shader);

        Material material = GetOrCreateDecalMaterial(shaderKey, shader);
        if (material == null) return;

        GameObject decalGO = CreateDecalGameObject(shaderKey, material);

        decalGO.transform.position = hit.point + hit.normal * SurfaceOffset;
        decalGO.transform.rotation = Quaternion.LookRotation(-hit.normal);
    }

    private static void SyncSelectedDecalScale()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;

        DecalProjector projector = selected.GetComponent<DecalProjector>();
        if (projector == null) return;

        Vector3 scale = selected.transform.localScale;

        if (Approximately(scale, Vector3.one)) return;

        Undo.RecordObject(projector, "Sync Decal Projector Size");
        Undo.RecordObject(selected.transform, "Reset Decal Transform Scale");

        projector.size = new Vector3(
            Mathf.Max(0.001f, projector.size.x * Mathf.Abs(scale.x)),
            Mathf.Max(0.001f, projector.size.y * Mathf.Abs(scale.y)),
            Mathf.Max(0.001f, projector.size.z * Mathf.Abs(scale.z))
        );

        selected.transform.localScale = Vector3.one;

        EditorUtility.SetDirty(projector);
        EditorUtility.SetDirty(selected);
        SceneView.RepaintAll();
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return Mathf.Approximately(a.x, b.x) &&
               Mathf.Approximately(a.y, b.y) &&
               Mathf.Approximately(a.z, b.z);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!IsBrushEnabled) return;

        Event e = Event.current;
        if (e == null) return;
        if (e.alt) return;

        bool isPaint =
            (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) &&
            e.button == 0;

        if (!isPaint)
        {
            if (e.type == EventType.MouseUp)
                State.HasLastBrushPosition = false;

            return;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        if (State.HasLastBrushPosition &&
            Vector3.Distance(hit.point, State.LastBrushPosition) < BrushSpacing)
        {
            return;
        }

        CreateBrushDecal(hit);

        State.LastBrushPosition = hit.point;
        State.HasLastBrushPosition = true;

        e.Use();
    }

    private static string GetShaderKey(Shader shader)
    {
        if (shader == null)
            return "Decal";

        int separator = shader.name.LastIndexOf('/');
        return separator >= 0 ? shader.name.Substring(separator + 1) : shader.name;
    }

    private static Material GetOrCreateDecalMaterial(string shaderKey, Shader shader)
    {
        EnsureFoldersExist();

        string matPath = $"{DecalsFolder}/{shaderKey}_Auto.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (material != null)
            return material;

        material = new Material(shader)
        {
            name = $"{shaderKey}_Auto"
        };

        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(matPath));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return material;
    }

    private static void EnsureFoldersExist()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets", "URPLabStudio");

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Materials");

        if (!AssetDatabase.IsValidFolder(DecalsFolder))
            AssetDatabase.CreateFolder(MaterialsFolder, "Decals");
    }
}
#endif
}
