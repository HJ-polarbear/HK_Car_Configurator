namespace URPLabStudio
{
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Lab_HybridBInteractionBridge : MonoBehaviour
{
    private Lab_InteractionUI interactionUI;
    private sealed class StaticState { public bool LightsEnabled; }
    private static readonly StaticState State = new StaticState();

    private void Awake()
    {
        interactionUI = FindObjectsByType<Lab_InteractionUI>(FindObjectsInactive.Include).FirstOrDefault();
        BindCamera("Camera_1", () => interactionUI?.SelectCloseUp());
        BindCamera("Camera_2", () => interactionUI?.SelectMidShot());
        BindCamera("Camera_3", () => interactionUI?.SelectFullShot());
        BindCamera("Camera_4", () => interactionUI?.SelectTurntable());
        BindCamera("Camera_5", () => interactionUI?.SelectFreeCamera());

        Bind("Control_LEFT_DOOR", () => ToggleOpenElement(Key.Numpad1, Key.Digit1));
        Bind("Control_RIGHT_DOOR", () => ToggleOpenElement(Key.Numpad2, Key.Digit2));
        Bind("Control_TRUNK", () => ToggleOpenElement(Key.Numpad3, Key.Digit3));
        Bind("Control_EXPLODE", ToggleExplode);
        Bind("Control_LIGHTS", ToggleLights);
        RemoveControl("Control_TURNTABLE");

        Lab_PostSelection posts = FindObjectsByType<Lab_PostSelection>(FindObjectsInactive.Include).FirstOrDefault();
        Bind("Post_Previous", () => posts?.PreviousPost());
        Bind("Post_Next", () => posts?.NextPost());
        BindPanelToggle();
        BindPaintMaterials();

    }

    private void BindCamera(string rowName, Action action)
    {
        Transform row = FindDeep(transform, rowName);
        if (row == null) return;
        Button button = row.GetComponentInChildren<Button>(true);
        if (button != null) button.onClick.AddListener(() => action());
    }

    private void Bind(string objectName, Action action)
    {
        Transform target = FindDeep(transform, objectName);
        if (target == null) return;
        Button button = target.GetComponentInChildren<Button>(true);
        if (button != null) button.onClick.AddListener(() => action());
    }

    private void RemoveControl(string objectName)
    {
        Transform target = FindDeep(transform, objectName);
        if (target == null) return;
        target.gameObject.SetActive(false);
        Destroy(target.gameObject);
    }

    private void BindPaintMaterials()
    {
        for (int index = 0; index < 6; index++)
        {
            int presetIndex = index;
            Transform target = FindDeep(transform, $"Material_{index + 1}");
            if (target == null) continue;

            Image image = target.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;

            Button button = target.GetComponent<Button>();
            if (button == null) button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                Lab_WavePaintController paint = FindObjectsByType<Lab_WavePaintController>(FindObjectsInactive.Exclude).FirstOrDefault();
                if (paint != null) paint.ApplyPreset(presetIndex);
            });
        }
    }

    private void BindPanelToggle()
    {
        Lab_HybridBPanelController controller = GetComponent<Lab_HybridBPanelController>();
        Transform tab = FindDeep(transform, "PanelToggleHitArea") ?? FindDeep(transform, "PanelArrowTab");
        Button button = tab != null ? tab.GetComponent<Button>() : null;
        if (controller == null || button == null) return;
        if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(controller.TogglePanel);
    }

    private static Transform FindDeep(Transform root, string objectName)
    {
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
    }

    private static void ToggleOpenElement(params Key[] keys)
    {
        foreach (Lab_OpenElement element in FindObjectsByType<Lab_OpenElement>(FindObjectsInactive.Exclude))
            if (keys.Contains(element.KeyboardKey)) element.Toggle();
    }

    private static void ToggleLights()
    {
        foreach (Lab_Light_On_Off lights in FindObjectsByType<Lab_Light_On_Off>(FindObjectsInactive.Exclude))
            lights.ToggleLights();
        State.LightsEnabled = !State.LightsEnabled;
        foreach (Lab_LightFlare flare in FindObjectsByType<Lab_LightFlare>(FindObjectsInactive.Include))
            flare.SetLights(State.LightsEnabled);
        Lab_ConfiguratorAudio.Instance?.PlayLights(State.LightsEnabled);
    }

    private static void ToggleExplode()
    {
        Lab_ExplodeController controller = FindObjectsByType<Lab_ExplodeController>(FindObjectsInactive.Exclude).FirstOrDefault();
        if (controller != null) controller.ToggleExplode();
    }

}
}
