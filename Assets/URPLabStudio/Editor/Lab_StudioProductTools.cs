namespace URPLabStudio.Editor
{
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class Lab_StudioSetupWizard : EditorWindow
{
    Transform orbitRoot;
    Transform contextRoot;
    Transform vehicleRoot;
    Material waveMaterial;
    Transform interiorRig;
    Transform interiorPivot;
    Camera interiorCamera;
    Camera exteriorCamera;

    [MenuItem("Tools/URPLab Studio/Setup Wizard")]
    static void Open()
    {
        GetWindow<Lab_StudioSetupWizard>("URPLab Setup");
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("URPLab Studio Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select the roots, then configure each product without writing code.", MessageType.Info);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Orbit U Panel", EditorStyles.boldLabel);
        orbitRoot = (Transform)EditorGUILayout.ObjectField("Orbit Root", orbitRoot, typeof(Transform), true);
        contextRoot = (Transform)EditorGUILayout.ObjectField("Context Root", contextRoot, typeof(Transform), true);
        using (new EditorGUI.DisabledScope(orbitRoot == null))
        {
            if (GUILayout.Button("Configure Orbit Panel", GUILayout.Height(28f)))
                ConfigureOrbit(orbitRoot, contextRoot);
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Wave Paint", EditorStyles.boldLabel);
        vehicleRoot = (Transform)EditorGUILayout.ObjectField("Vehicle Root", vehicleRoot, typeof(Transform), true);
        waveMaterial = (Material)EditorGUILayout.ObjectField("Wave Material", waveMaterial, typeof(Material), false);
        using (new EditorGUI.DisabledScope(vehicleRoot == null || waveMaterial == null))
        {
            if (GUILayout.Button("Configure Wave Paint", GUILayout.Height(28f)))
                ConfigureWavePaint(vehicleRoot, waveMaterial);
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Interior Camera", EditorStyles.boldLabel);
        interiorRig = (Transform)EditorGUILayout.ObjectField("Interior Rig", interiorRig, typeof(Transform), true);
        interiorPivot = (Transform)EditorGUILayout.ObjectField("Look Pivot", interiorPivot, typeof(Transform), true);
        interiorCamera = (Camera)EditorGUILayout.ObjectField("Interior Camera", interiorCamera, typeof(Camera), true);
        exteriorCamera = (Camera)EditorGUILayout.ObjectField("Exterior Camera", exteriorCamera, typeof(Camera), true);
        using (new EditorGUI.DisabledScope(interiorRig == null || interiorPivot == null ||
            interiorCamera == null || exteriorCamera == null))
        {
            if (GUILayout.Button("Configure Interior Camera", GUILayout.Height(28f)))
                ConfigureInteriorCamera(interiorRig, interiorPivot, interiorCamera, exteriorCamera);
        }
        if (GUILayout.Button("Auto Find Interior Camera"))
            ConfigureInteriorCameraInOpenScene();
    }

    public static Lab_OrbitPanelController ConfigureOrbit(Transform ring, Transform panels)
    {
        if (ring == null)
            throw new ArgumentNullException(nameof(ring));
        Lab_OrbitPanelController controller = ring.GetComponent<Lab_OrbitPanelController>();
        if (controller == null)
            controller = Undo.AddComponent<Lab_OrbitPanelController>(ring.gameObject);

        List<Lab_OrbitPanelController.CategoryPanel> bindings = DiscoverPanels(panels);
        controller.Configure(ring, panels, bindings);
        ConfigureCategorySlots(controller, ring);
        ConfigureActionSlots(controller, panels);
        controller.RebuildSlotCache();
        EditorUtility.SetDirty(controller);
        MarkSceneDirty(controller.gameObject);
        Selection.activeObject = controller;
        return controller;
    }

    public static Lab_WavePaintController ConfigureWavePaint(Transform vehicle, Material template)
    {
        if (vehicle == null)
            throw new ArgumentNullException(nameof(vehicle));
        Lab_WavePaintController controller = vehicle.GetComponentInChildren<Lab_WavePaintController>(true);
        if (controller == null)
        {
            GameObject host = new GameObject("URP_Wave_Paint");
            Undo.RegisterCreatedObjectUndo(host, "Create Wave Paint Controller");
            host.transform.SetParent(vehicle, false);
            controller = Undo.AddComponent<Lab_WavePaintController>(host);
        }
        controller.Configure(vehicle, template);
        controller.ScanPaintTargets();
        EditorUtility.SetDirty(controller);
        MarkSceneDirty(controller.gameObject);
        Selection.activeObject = controller;
        return controller;
    }

    [MenuItem("Tools/URPLab Studio/Configure Interior Camera")]
    public static void ConfigureInteriorCameraInOpenScene()
    {
        Transform rig = FindTransform("Interior_Camera_Rig");
        Transform pivot = FindTransform("Interior_Camera_Pivot");
        Camera interior = FindTransform("Interior_Camera")?.GetComponent<Camera>();
        Camera exterior = FindTransform("Main Camera")?.GetComponent<Camera>();

        if (rig == null || pivot == null || interior == null || exterior == null)
        {
            Debug.LogError("Interior Camera setup requires Interior_Camera_Rig, Interior_Camera_Pivot, Interior_Camera, and Main Camera.");
            return;
        }

        ConfigureInteriorCamera(rig, pivot, interior, exterior);
        Debug.Log("URPLab Studio: Interior Camera configured.");
    }

    public static Lab_InteriorCameraController ConfigureInteriorCamera(Transform rig, Transform pivot,
        Camera interior, Camera exterior)
    {
        if (rig == null)
            throw new ArgumentNullException(nameof(rig));
        if (pivot == null)
            throw new ArgumentNullException(nameof(pivot));
        if (interior == null)
            throw new ArgumentNullException(nameof(interior));
        if (exterior == null)
            throw new ArgumentNullException(nameof(exterior));

        Lab_OrbitPanelController orbit = UnityEngine.Object.FindAnyObjectByType<Lab_OrbitPanelController>(
            FindObjectsInactive.Include);
        if (orbit == null)
            throw new InvalidOperationException("No Orbit Panel Controller was found.");

        Lab_InteriorCameraController controller = rig.GetComponent<Lab_InteriorCameraController>();
        if (controller == null)
            controller = Undo.AddComponent<Lab_InteriorCameraController>(rig.gameObject);

        AudioListener exteriorListener = exterior.GetComponent<AudioListener>();
        AudioListener interiorListener = interior.GetComponent<AudioListener>();
        if (interiorListener == null)
            interiorListener = Undo.AddComponent<AudioListener>(interior.gameObject);

        Button backButton = EnsureInteriorBackButton();
        Behaviour exteriorInput = exterior.GetComponent<Lab_CameraDirector>();
        controller.Configure(exterior, interior, pivot, exteriorInput, exteriorListener, interiorListener,
            orbit, orbit.gameObject, orbit.ContextRoot != null ? orbit.ContextRoot.gameObject : null, backButton);
        orbit.SetInteriorCameraController(controller);

        Undo.RecordObject(interior, "Configure Interior Camera");
        interior.enabled = false;
        interior.gameObject.tag = "Untagged";
        Undo.RecordObject(interiorListener, "Configure Interior Audio");
        interiorListener.enabled = false;

        foreach (MonoBehaviour behaviour in interior.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null || behaviour.GetType().Name != "PlanarReflections")
                continue;
            Undo.RecordObject(behaviour, "Disable Duplicate Interior Reflection");
            behaviour.enabled = false;
            EditorUtility.SetDirty(behaviour);
        }

        if (backButton != null)
            backButton.gameObject.SetActive(false);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(orbit);
        EditorUtility.SetDirty(interior);
        EditorUtility.SetDirty(interiorListener);
        MarkSceneDirty(rig.gameObject);
        Selection.activeObject = controller;
        return controller;
    }

    public static List<Lab_OrbitPanelController.CategoryPanel> DiscoverPanels(Transform panelRoot)
    {
        List<Lab_OrbitPanelController.CategoryPanel> bindings = new List<Lab_OrbitPanelController.CategoryPanel>();
        if (panelRoot == null)
            return bindings;
        foreach (Transform child in panelRoot)
        {
            string category = ResolveCategory(child.name);
            if (string.IsNullOrEmpty(category))
                continue;
            bindings.Add(new Lab_OrbitPanelController.CategoryPanel
            {
                categoryId = category,
                panel = child.gameObject
            });
        }
        return bindings;
    }

    public static void ConfigureCategorySlots(Lab_OrbitPanelController controller, Transform ring)
    {
        if (controller == null || ring == null)
            return;
        foreach (Transform candidate in ring.GetComponentsInChildren<Transform>(true))
        {
            string category = ResolveCategory(candidate.name);
            if (string.IsNullOrEmpty(category) || candidate.GetComponentsInChildren<Collider>(true).Length == 0)
                continue;
            if (candidate.parent != ring && !candidate.name.StartsWith("Lab_Segment_", StringComparison.OrdinalIgnoreCase))
                continue;
            Lab_OrbitControlSlot slot = candidate.GetComponent<Lab_OrbitControlSlot>();
            if (slot == null)
                slot = Undo.AddComponent<Lab_OrbitControlSlot>(candidate.gameObject);
            slot.ConfigureCategory(controller, category);
            EditorUtility.SetDirty(slot);
        }
    }

    public static void ConfigureActionSlots(Lab_OrbitPanelController controller, Transform panels)
    {
        if (controller == null || panels == null)
            return;
        foreach (Button button in panels.GetComponentsInChildren<Button>(true))
        {
            Lab_OrbitControlSlot.ActionType action = ResolveAction(button.transform);
            int option = ResolveOptionIndex(button.name);
            Image bar = button.transform.Find("SelectionBar")?.GetComponent<Image>();
            Color color = button.image != null ? button.image.color : Color.white;
            Lab_OrbitControlSlot slot = button.GetComponent<Lab_OrbitControlSlot>();
            if (slot == null)
                slot = Undo.AddComponent<Lab_OrbitControlSlot>(button.gameObject);
            slot.ConfigureAction(controller, action, option, color, bar);
            EditorUtility.SetDirty(slot);
        }
    }

    static Transform FindTransform(string objectName)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .FirstOrDefault(item => item.name == objectName);
    }

    static Button EnsureInteriorBackButton()
    {
        Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        Button existing = buttons.FirstOrDefault(button => button.name == "Interior_Back");
        if (existing != null)
            return existing;

        Button template = buttons.FirstOrDefault(button => button.name == "Exit");
        if (template == null)
        {
            Debug.LogWarning("Interior Camera: no Exit button template was found. Assign a Back Button manually.");
            return null;
        }

        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
        Undo.RegisterCreatedObjectUndo(clone, "Create Interior Back Button");
        clone.name = "Interior_Back";

        Button backButton = clone.GetComponent<Button>();
        Undo.RecordObject(backButton, "Configure Interior Back Button");
        backButton.onClick = new Button.ButtonClickedEvent();

        RectTransform backRect = clone.transform as RectTransform;
        RectTransform templateRect = template.transform as RectTransform;
        if (backRect != null && templateRect != null)
            backRect.anchoredPosition = templateRect.anchoredPosition + Vector2.left * 144f;

        TMP_Text label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Undo.RecordObject(label, "Configure Interior Back Label");
            label.text = "<   BACK";
            EditorUtility.SetDirty(label);
        }

        clone.SetActive(false);
        EditorUtility.SetDirty(backButton);
        return backButton;
    }

    static Lab_OrbitControlSlot.ActionType ResolveAction(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            string category = ResolveCategory(current.name);
            if (category == "WHEELS") return Lab_OrbitControlSlot.ActionType.Wheel;
            if (category == "EXTERIOR") return Lab_OrbitControlSlot.ActionType.Paint;
            if (category == "CAMERA") return Lab_OrbitControlSlot.ActionType.Camera;
            if (category == "DOORS") return Lab_OrbitControlSlot.ActionType.Door;
            if (category == "LIGHTS") return Lab_OrbitControlSlot.ActionType.Lights;
            if (category == "EXPLODE") return Lab_OrbitControlSlot.ActionType.Explode;
            current = current.parent;
        }
        return Lab_OrbitControlSlot.ActionType.Custom;
    }

    static int ResolveOptionIndex(string objectName)
    {
        int separator = objectName.LastIndexOf('_');
        return separator >= 0 && int.TryParse(objectName.Substring(separator + 1), out int index) ? index : 0;
    }

    static string ResolveCategory(string value)
    {
        string upper = value.ToUpperInvariant();
        string[] categories = { "WHEELS", "EXTERIOR", "INTERIOR", "CAMERA", "DOORS", "LIGHTS", "EXPLODE" };
        return categories.FirstOrDefault(upper.Contains) ?? string.Empty;
    }

    static void MarkSceneDirty(GameObject target)
    {
        if (target.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(target.scene);
    }
}

[CustomEditor(typeof(Lab_OrbitPanelController))]
public sealed class Lab_OrbitPanelControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Orbit U Panel", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitRoot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("contextRoot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("categoryPanels"), true);

        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("Motion", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("speedBlendDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resumeDelay"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverLift"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("liftSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedTint"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("panelMotionDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("panelRiseDistance"));
        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("Automatic Panel Close", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("closeWhenAnotherSlotFacesCamera"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("frontFacingAngle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("frontFacingHoldDuration"));

        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("Interior Camera", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("interiorCameraController"));
        serializedObject.ApplyModifiedProperties();

        Lab_OrbitPanelController controller = (Lab_OrbitPanelController)target;
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto Configure"))
                Lab_StudioSetupWizard.ConfigureOrbit(controller.OrbitRoot != null ? controller.OrbitRoot : controller.transform,
                    controller.ContextRoot);
            if (GUILayout.Button("Validate"))
                EditorUtility.DisplayDialog("Orbit U Panel", controller.ValidateConfiguration(), "OK");
        }
        EditorGUILayout.HelpBox(controller.ValidateConfiguration(),
            controller.ValidateConfiguration() == "Setup is valid." ? MessageType.Info : MessageType.Warning);
    }
}

[CustomEditor(typeof(Lab_InteriorCameraController))]
public sealed class Lab_InteriorCameraControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Interior Camera", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("exteriorCamera"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("interiorCamera"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lookPivot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("exteriorInputController"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("exteriorAudioListener"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("interiorAudioListener"));

        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("Interface", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitPanelController"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitInterfaceRoot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("contextInterfaceRoot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("backButton"));

        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("Interior Look", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lookSensitivity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("yawLimits"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pitchLimits"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resetViewOnEnter"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allowEscapeToExit"));
        serializedObject.ApplyModifiedProperties();

        Lab_InteriorCameraController controller = (Lab_InteriorCameraController)target;
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto Configure Scene"))
                Lab_StudioSetupWizard.ConfigureInteriorCameraInOpenScene();
            if (GUILayout.Button("Validate"))
                EditorUtility.DisplayDialog("Interior Camera", controller.ValidateConfiguration(), "OK");
        }

        string validation = controller.ValidateConfiguration();
        EditorGUILayout.HelpBox(validation,
            validation == "Setup is valid." ? MessageType.Info : MessageType.Warning);
    }
}

[CustomEditor(typeof(Lab_WavePaintController))]
public sealed class Lab_WavePaintControllerEditor : UnityEditor.Editor
{
    bool showAdvanced;
    bool showTargets;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Wave Paint Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("vehicleRoot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("waveMaterialTemplate"));

        Lab_WavePaintController controller = (Lab_WavePaintController)target;
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"Paint Targets: {controller.PaintTargets.Count}");
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan Paint Targets"))
            {
                Undo.RecordObject(controller, "Scan Paint Targets");
                controller.ScanPaintTargets();
                EditorUtility.SetDirty(controller);
            }
            if (GUILayout.Button("Validate"))
                EditorUtility.DisplayDialog("Wave Paint", controller.ValidateConfiguration(), "OK");
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Wave", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("waveSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("endOffset"));
        if (GUILayout.Button("Auto Fit Range"))
        {
            Undo.RecordObject(controller, "Auto Fit Wave Range");
            controller.AutoFitWaveRange();
            EditorUtility.SetDirty(controller);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("paintPresets"), true);
        showTargets = EditorGUILayout.Foldout(showTargets, "Paint Target Details", true);
        if (showTargets)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("paintTargets"), true);
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Detection", true);
        if (showAdvanced)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("paintMaterialPrefixes"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("compensateTransformScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("referenceScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumScaleFactor"));
        }
        serializedObject.ApplyModifiedProperties();

        string validation = controller.ValidateConfiguration();
        EditorGUILayout.HelpBox(validation, validation == "Setup is valid." ? MessageType.Info : MessageType.Warning);
    }
}
}