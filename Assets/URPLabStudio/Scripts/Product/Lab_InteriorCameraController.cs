namespace URPLabStudio
{
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Lab_InteriorCameraController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] Camera exteriorCamera;
    [SerializeField] Camera interiorCamera;
    [SerializeField] Transform lookPivot;
    [SerializeField] Behaviour exteriorInputController;
    [SerializeField] AudioListener exteriorAudioListener;
    [SerializeField] AudioListener interiorAudioListener;

    [Header("Interface")]
    [SerializeField] Lab_OrbitPanelController orbitPanelController;
    [SerializeField] GameObject orbitInterfaceRoot;
    [SerializeField] GameObject contextInterfaceRoot;
    [SerializeField] Button backButton;
    [SerializeField] Button exitButton;
    [SerializeField] Lab_Nav3D navigation3D;

    [Header("Interior Look")]
    [SerializeField, Min(.01f)] float lookSensitivity = .12f;
    [SerializeField] Vector2 yawLimits = new Vector2(-110f, 110f);
    [SerializeField] Vector2 pitchLimits = new Vector2(-30f, 25f);
    [SerializeField] bool resetViewOnEnter = true;
    [SerializeField] bool allowEscapeToExit = true;

    Quaternion initialPivotRotation;
    Pointer pointer;
    float yaw;
    bool dragging;
    bool initialized;
    bool exteriorCameraWasEnabled;
    bool exteriorInputWasEnabled;
    bool exteriorListenerWasEnabled;

    public bool IsInteriorActive { get; private set; }
    public Camera ExteriorCamera => exteriorCamera;
    public Camera InteriorCamera => interiorCamera;
    public Transform LookPivot => lookPivot;
    public Button BackButton => backButton;

    void Awake()
    {
        Initialize();
        SetupNavigationControls();
        ApplyExteriorState();
    }

    void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(ExitInterior);
    }

    void Update()
    {
        if (!IsInteriorActive)
            return;

        if (allowEscapeToExit && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitInterior();
            return;
        }

        UpdateLookInput();
    }

    public void Configure(Camera exterior, Camera interior, Transform pivot, Behaviour exteriorInput,
        AudioListener exteriorListener, AudioListener interiorListener, Lab_OrbitPanelController orbitPanel,
        GameObject orbitRoot, GameObject contextRoot, Button exitButton)
    {
        exteriorCamera = exterior;
        interiorCamera = interior;
        lookPivot = pivot;
        exteriorInputController = exteriorInput;
        exteriorAudioListener = exteriorListener;
        interiorAudioListener = interiorListener;
        orbitPanelController = orbitPanel;
        orbitInterfaceRoot = orbitRoot;
        contextInterfaceRoot = contextRoot;
        backButton = exitButton;
        initialized = false;
    }

    public void EnterInterior()
    {
        Initialize();
        if (IsInteriorActive || interiorCamera == null || exteriorCamera == null || lookPivot == null)
            return;

        exteriorCameraWasEnabled = exteriorCamera.enabled;
        exteriorInputWasEnabled = exteriorInputController != null && exteriorInputController.enabled;
        exteriorListenerWasEnabled = exteriorAudioListener != null && exteriorAudioListener.enabled;

        IsInteriorActive = true;
        dragging = false;
        if (resetViewOnEnter)
        {
            yaw = 0f;
            lookPivot.localRotation = initialPivotRotation;
        }

        if (exteriorInputController != null)
            exteriorInputController.enabled = false;
        if (exteriorAudioListener != null)
            exteriorAudioListener.enabled = false;
        exteriorCamera.enabled = false;
        exteriorCamera.gameObject.tag = "Untagged";

        interiorCamera.gameObject.tag = "MainCamera";
        interiorCamera.enabled = true;
        if (interiorAudioListener != null)
            interiorAudioListener.enabled = true;

        if (contextInterfaceRoot != null)
            contextInterfaceRoot.SetActive(false);
        if (orbitInterfaceRoot != null)
            orbitInterfaceRoot.SetActive(false);
        if (backButton != null)
            backButton.gameObject.SetActive(false);
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
        navigation3D?.ShowInterior();
    }

    public void ExitInterior()
    {
        if (!IsInteriorActive)
            return;

        IsInteriorActive = false;
        dragging = false;

        if (interiorAudioListener != null)
            interiorAudioListener.enabled = false;
        if (interiorCamera != null)
        {
            interiorCamera.enabled = false;
            interiorCamera.gameObject.tag = "Untagged";
        }

        if (exteriorCamera != null)
        {
            exteriorCamera.gameObject.tag = "MainCamera";
            exteriorCamera.enabled = exteriorCameraWasEnabled;
        }
        if (exteriorAudioListener != null)
            exteriorAudioListener.enabled = exteriorListenerWasEnabled;
        if (exteriorInputController != null)
            exteriorInputController.enabled = exteriorInputWasEnabled;

        if (orbitInterfaceRoot != null)
            orbitInterfaceRoot.SetActive(true);
        if (contextInterfaceRoot != null)
            contextInterfaceRoot.SetActive(false);
        if (backButton != null)
            backButton.gameObject.SetActive(false);
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
        navigation3D?.ShowExterior();

        orbitPanelController?.ClearSelection();
    }

    public string ValidateConfiguration()
    {
        if (exteriorCamera == null)
            return "Exterior Camera is not assigned.";
        if (interiorCamera == null)
            return "Interior Camera is not assigned.";
        if (lookPivot == null)
            return "Look Pivot is not assigned.";
        if (orbitPanelController == null)
            return "Orbit Panel Controller is not assigned.";
        if (navigation3D == null)
            return "Lab Nav 3D is not assigned.";
        return "Setup is valid.";
    }

    void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        pointer = Pointer.current;
        initialPivotRotation = lookPivot != null ? lookPivot.localRotation : Quaternion.identity;
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ExitInterior);
            backButton.onClick.AddListener(ExitInterior);
        }
    }

    void ApplyExteriorState()
    {
        IsInteriorActive = false;
        dragging = false;

        if (interiorAudioListener != null)
            interiorAudioListener.enabled = false;
        if (interiorCamera != null)
        {
            interiorCamera.enabled = false;
            interiorCamera.gameObject.tag = "Untagged";
        }
        if (exteriorCamera != null)
        {
            exteriorCamera.enabled = true;
            exteriorCamera.gameObject.tag = "MainCamera";
        }
        if (exteriorAudioListener != null)
            exteriorAudioListener.enabled = true;
        if (exteriorInputController != null)
            exteriorInputController.enabled = true;
        if (orbitInterfaceRoot != null)
            orbitInterfaceRoot.SetActive(true);
        if (contextInterfaceRoot != null)
            contextInterfaceRoot.SetActive(false);
        if (backButton != null)
            backButton.gameObject.SetActive(false);
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
        navigation3D?.ShowExterior();
    }

    public void InvokeExit()
    {
        if (exitButton != null)
        {
            exitButton.onClick.Invoke();
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetNavigation3D(Lab_Nav3D value)
    {
        navigation3D = value;
    }

    void SetupNavigationControls()
    {
        if (navigation3D == null)
            navigation3D = GetComponentInChildren<Lab_Nav3D>(true);
        navigation3D?.Initialize(this);
    }

    void UpdateLookInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || lookPivot == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
            dragging = !IsPointerOverUI();
        if (mouse.leftButton.wasReleasedThisFrame)
            dragging = false;
        if (!dragging || !mouse.leftButton.isPressed)
            return;

        Vector2 delta = mouse.delta.ReadValue();
        if (Mathf.Abs(delta.x) < 0.0001f)
            return;

        yaw = Mathf.Clamp(yaw + delta.x * lookSensitivity, yawLimits.x, yawLimits.y);
        lookPivot.localRotation = initialPivotRotation * Quaternion.Euler(0f, yaw, 0f);
    }
    static bool IsPointerOverUI()
    {
        EventSystem eventSystem = EventSystem.current;
        Pointer currentPointer = Pointer.current;
        if (eventSystem == null || currentPointer == null)
            return false;

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = currentPointer.position.ReadValue()
        };
        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }
        return false;
    }
}
}
