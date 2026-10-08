namespace URPLabStudio
{
#pragma warning disable 0649

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Lab_InteractionUI : MonoBehaviour
{
    [Header("Camera UI")]
    [SerializeField] Lab_CameraDirector cameraDirector;
    [FormerlySerializedAs("toggleMixed")]
    [SerializeField] Toggle turntableToggle;
    [FormerlySerializedAs("toggleFace")]
    [SerializeField] Toggle closeUpToggle;
    [FormerlySerializedAs("cameraFace")]
    [FormerlySerializedAs("legacyCloseUpCamera")]
    [SerializeField] Camera closeUpCamera;
    [FormerlySerializedAs("toggleMidshot")]
    [SerializeField] Toggle midShotToggle;
    [FormerlySerializedAs("cameraMidshot")]
    [FormerlySerializedAs("legacyMidShotCamera")]
    [SerializeField] Camera midShotCamera;
    [FormerlySerializedAs("toggleBody")]
    [SerializeField] Toggle fullShotToggle;
    [FormerlySerializedAs("cameraBody")]
    [FormerlySerializedAs("legacyFullShotCamera")]
    [SerializeField] Camera fullShotCamera;

    [System.NonSerialized] bool initialized;

    public void SelectCloseUp()
    {
        if (cameraDirector != null) cameraDirector.SelectClose();
        SetToggleOn(closeUpToggle);
    }

    public void SelectMidShot()
    {
        if (cameraDirector != null) cameraDirector.SelectMid();
        SetToggleOn(midShotToggle);
    }

    public void SelectFullShot()
    {
        if (cameraDirector != null) cameraDirector.SelectFull();
        SetToggleOn(fullShotToggle);
    }

    public void SelectTurntable()
    {
        if (cameraDirector != null) cameraDirector.SelectTurntable();
        SetToggleOn(turntableToggle);
    }

    public void SelectFreeCamera()
    {
        if (cameraDirector != null)
            cameraDirector.SelectOrbit();
    }

    void OnEnable()
    {
        if (!initialized) Initialize();
    }

    void Start()
    {
        SetDefaults();
    }

    void Initialize()
    {
        if (cameraDirector == null)
            cameraDirector = FindAnyObjectByType<Lab_CameraDirector>();

        ToggleGroup cameraToggleGroup =
            closeUpToggle != null ? closeUpToggle.group :
            midShotToggle != null ? midShotToggle.group :
            fullShotToggle != null ? fullShotToggle.group :
            turntableToggle != null ? turntableToggle.group : null;
        if (cameraToggleGroup != null)
            cameraToggleGroup.allowSwitchOff = true;

        SetupCameraToggle(closeUpCamera, closeUpToggle, Lab_CameraDirector.CameraMode.Close);
        SetupCameraToggle(midShotCamera, midShotToggle, Lab_CameraDirector.CameraMode.Mid);
        SetupCameraToggle(fullShotCamera, fullShotToggle, Lab_CameraDirector.CameraMode.Full);
        SetupCameraToggle(null, turntableToggle, Lab_CameraDirector.CameraMode.Turntable);

        initialized = true;
    }

    void SetDefaults()
    {
        SetToggleWithoutNotify(closeUpToggle, false);
        SetToggleWithoutNotify(midShotToggle, false);
        SetToggleWithoutNotify(fullShotToggle, false);
        SetToggleWithoutNotify(turntableToggle, false);

        if (cameraDirector != null)
            cameraDirector.KeepCurrentView();
    }

    void Update()
    {
        if (cameraDirector == null)
        {
            if (KeyWasPressed(Key.R)) SetToggleOn(turntableToggle);
            if (KeyWasPressed(Key.E)) SetToggleOn(fullShotToggle);
            if (KeyWasPressed(Key.W)) SetToggleOn(midShotToggle);
            if (KeyWasPressed(Key.Q)) SetToggleOn(closeUpToggle);
        }

    }

    bool KeyWasPressed(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        var keyControl = keyboard[key];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    void SetupCameraToggle(
        Camera legacyTarget,
        Toggle toggle,
        Lab_CameraDirector.CameraMode mode)
    {
        if (toggle == null) return;

        toggle.onValueChanged.AddListener(enable =>
        {
            if (!enable) return;

            if (cameraDirector != null)
                SelectCameraMode(mode);
        });

        if (legacyTarget != null)
            legacyTarget.gameObject.SetActive(false);
    }

    void SelectCameraMode(Lab_CameraDirector.CameraMode mode)
    {
        switch (mode)
        {
            case Lab_CameraDirector.CameraMode.Close:
                cameraDirector.SelectClose();
                break;
            case Lab_CameraDirector.CameraMode.Mid:
                cameraDirector.SelectMid();
                break;
            case Lab_CameraDirector.CameraMode.Full:
                cameraDirector.SelectFull();
                break;
            case Lab_CameraDirector.CameraMode.Turntable:
                cameraDirector.SelectTurntable();
                break;
            case Lab_CameraDirector.CameraMode.Orbit:
                cameraDirector.SelectOrbit();
                break;
        }
    }

    void SetToggleWithoutNotify(Toggle toggle, bool shouldBeOn)
    {
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(shouldBeOn);
    }

    void SetToggleOn(Toggle toggle)
    {
        if (toggle != null)
            toggle.isOn = true;
    }

}
}
