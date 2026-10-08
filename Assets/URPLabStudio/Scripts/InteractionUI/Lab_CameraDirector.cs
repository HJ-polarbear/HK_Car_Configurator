namespace URPLabStudio
{
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Lab_CameraDirector : MonoBehaviour
{
    private struct TurntableState
    {
        public bool Pending;
        public Vector2 Angles;
        public float Distance;
    }

    private static TurntableState pendingTurntableState;
    public enum CameraMode { Current, Close, Mid, Full, Turntable, Orbit }

    [Header("Camera And Framing")]
    [SerializeField] Camera targetCamera;
    [SerializeField] Transform target;
    [SerializeField] Transform closeShot;
    [SerializeField] Transform midShot;
    [SerializeField] Transform fullShot;

    [Header("Startup")]
    [SerializeField] CameraMode startMode = CameraMode.Current;

    [Header("Shot Transition")]
    [SerializeField, Min(0f)] float transitionDuration = 0.5f;
    [SerializeField] AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Orbit")]
    [SerializeField, Min(0f)] float orbitDistance = 5f;
    [SerializeField] Vector2 orbitAngles = new Vector2(15f, 0f);
    [SerializeField] Vector2 pitchLimits = new Vector2(-20f, 70f);
    [SerializeField, Min(0f)] float rotationSpeed = 0.35f;
    [SerializeField, Min(0f)] float zoomSpeed = 0.02f;
    [SerializeField, Min(0.01f)] float minimumOrbitDistance = 3f;
    [SerializeField, Min(0.01f)] float maximumOrbitDistance = 10f;
    [SerializeField] Vector3 orbitTargetOffset;
    [SerializeField] bool preventOrbitBelowFloor = true;
    [SerializeField] float minimumOrbitCameraY = 0.2f;

    [Header("Turntable")]
    [SerializeField, Min(0f)] float turntableSpeed = 8f;
    [SerializeField] bool turntableClockwise = true;

    [Header("Input")]
    [SerializeField] bool enableKeyboardShortcuts = true;
    [SerializeField] bool clickStopsTurntable = true;
    [SerializeField, Min(0f)] float clickStopDelay = 0.25f;

    public CameraMode Mode { get; private set; } = CameraMode.Current;

    /// <summary>Called immediately before a Studio scene is replaced.</summary>
    public static void RememberTurntableForSceneChange()
    {
        Lab_CameraDirector director = FindAnyObjectByType<Lab_CameraDirector>();
        pendingTurntableState = new TurntableState
        {
            Pending = director != null && director.Mode == CameraMode.Turntable,
            Angles = director != null ? director.orbitAngles : default,
            Distance = director != null ? director.orbitDistance : 0f
        };
    }

    Transform activeShot;
    Vector3 transitionStartPosition;
    Quaternion transitionStartRotation;
    float transitionTime;
    Mouse mouse;
    Keyboard keyboard;
    float turntableStartedAt;
    bool waitForTurntablePointerRelease;
    bool skipNextOrbitInputFrame;
    bool turntableActive;
    float shotTurntableYaw;
    float runtimeMaximumDistance;
    float runtimeMinimumPitch;
    float runtimeMaximumPitch;

    Vector3 FocusPosition => target.position + orbitTargetOffset;
    float MinimumDistance => Mathf.Min(minimumOrbitDistance, maximumOrbitDistance);
    float MaximumDistance => Mathf.Max(minimumOrbitDistance, maximumOrbitDistance);
    float MinimumPitch => Mathf.Min(pitchLimits.x, pitchLimits.y);
    float MaximumPitch => Mathf.Max(pitchLimits.x, pitchLimits.y);

    void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        mouse = Mouse.current;
        keyboard = Keyboard.current;
        runtimeMaximumDistance = MaximumDistance;
        runtimeMinimumPitch = MinimumPitch;
        runtimeMaximumPitch = MaximumPitch;

        if (targetCamera == null)
        {
            enabled = false;
            Debug.LogError(nameof(Lab_CameraDirector) + " requires a Camera.", this);
            return;
        }

        Mode = CameraMode.Current;
        activeShot = null;

        switch (startMode)
        {
            case CameraMode.Close: SelectClose(); break;
            case CameraMode.Mid: SelectMid(); break;
            case CameraMode.Full: SelectFull(); break;
            case CameraMode.Turntable: SelectTurntable(); break;
            case CameraMode.Orbit: SelectOrbit(); break;
        }
    }

    IEnumerator Start()
    {
        if (!pendingTurntableState.Pending)
            yield break;

        TurntableState state = pendingTurntableState;
        pendingTurntableState.Pending = false;
        yield return null;

        orbitAngles = state.Angles;
        orbitDistance = Mathf.Clamp(state.Distance, MinimumDistance, Mathf.Max(MaximumDistance, state.Distance));
        runtimeMaximumDistance = Mathf.Max(MaximumDistance, orbitDistance);
        runtimeMinimumPitch = Mathf.Min(MinimumPitch, orbitAngles.x);
        runtimeMaximumPitch = Mathf.Max(MaximumPitch, orbitAngles.x);
        activeShot = null;
        turntableActive = true;
        shotTurntableYaw = 0f;
        Mode = CameraMode.Turntable;
        turntableStartedAt = Time.unscaledTime;
        waitForTurntablePointerRelease = true;
        UpdateOrbit();
    }

    void Update()
    {
        HandleKeyboardShortcuts();

        bool canEnterOrbitFromClick =
            Mode == CameraMode.Current ||
            Mode == CameraMode.Close ||
            Mode == CameraMode.Mid ||
            Mode == CameraMode.Full;

        if (canEnterOrbitFromClick &&
            PointerPressedThisFrame() &&
            IsOrbitClickArea() &&
            !IsPointerOverUI())
        {
            SelectOrbit();
            return;
        }

        if (Mode != CameraMode.Turntable || !clickStopsTurntable)
            return;

        if (waitForTurntablePointerRelease)
        {
            mouse ??= Mouse.current;
            bool pointerHeld = mouse != null &&
                (mouse.leftButton.isPressed ||
                 mouse.rightButton.isPressed ||
                 mouse.middleButton.isPressed);

            if (!pointerHeld &&
                Time.unscaledTime - turntableStartedAt >= Mathf.Max(clickStopDelay, 1.25f))
                waitForTurntablePointerRelease = false;

            return;
        }

        if (PointerPressedThisFrame() && !IsPointerOverUI())
            Mode = CameraMode.Orbit;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        if (Mode == CameraMode.Orbit)
        {
            HandleOrbitInput();
            return;
        }

        if (Mode == CameraMode.Turntable)
        {
            float yawDelta =
                (turntableClockwise ? 1f : -1f) * turntableSpeed * Time.deltaTime;
            orbitAngles.y += yawDelta;
            ApplyIncrementalOrbit(yawDelta, 0f);
            return;
        }

        if (activeShot == null) return;

        float normalizedTime = transitionDuration <= 0f ? 1f :
            Mathf.Clamp01(transitionTime / transitionDuration);
        float curvedTime = transitionCurve.Evaluate(normalizedTime);

        Vector3 position = Vector3.LerpUnclamped(
            transitionStartPosition,
            activeShot.position,
            curvedTime);
        Quaternion rotation = Quaternion.SlerpUnclamped(
            transitionStartRotation,
            activeShot.rotation,
            curvedTime);

        if (turntableActive)
        {
            float yawDelta =
                (turntableClockwise ? 1f : -1f) * turntableSpeed * Time.deltaTime;
            shotTurntableYaw += yawDelta;
            Quaternion yawRotation = Quaternion.AngleAxis(shotTurntableYaw, Vector3.up);
            Vector3 focus = FocusPosition;
            position = focus + yawRotation * (position - focus);
            rotation = yawRotation * rotation;
        }

        targetCamera.transform.SetPositionAndRotation(position, rotation);
        transitionTime += Time.deltaTime;
    }

    public void KeepCurrentView()
    {
        Mode = CameraMode.Current;
        activeShot = null;
        skipNextOrbitInputFrame = false;
        turntableActive = false;
        shotTurntableYaw = 0f;
    }

    public void SelectClose() => MoveToShot(CameraMode.Close, closeShot);
    public void SelectMid() => MoveToShot(CameraMode.Mid, midShot);
    public void SelectFull() => MoveToShot(CameraMode.Full, fullShot);
    public void SelectOrbit() => EnterOrbit(CameraMode.Orbit);
    public void SelectTurntable() => EnterOrbit(CameraMode.Turntable);
    public void SelectCloseUp() => SelectClose();
    public void SelectMidShot() => SelectMid();
    public void SelectFullShot() => SelectFull();
    public void SelectFreeCamera() => SelectOrbit();

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (Mode == CameraMode.Orbit || Mode == CameraMode.Turntable)
            SyncOrbitFromCurrentView();
    }

    void MoveToShot(CameraMode newMode, Transform shot)
    {
        if (targetCamera == null || shot == null)
        {
            Debug.LogWarning(nameof(Lab_CameraDirector) + ": " + newMode + " shot is not assigned.", this);
            return;
        }

        bool continueTurntable =
            Mode == CameraMode.Turntable || turntableActive;

        Vector3 currentPosition = targetCamera.transform.position;
        Quaternion currentRotation = targetCamera.transform.rotation;
        if (continueTurntable && Mathf.Abs(shotTurntableYaw) > 0.0001f)
        {
            Quaternion inverseYaw = Quaternion.AngleAxis(-shotTurntableYaw, Vector3.up);
            Vector3 focus = FocusPosition;
            currentPosition = focus + inverseYaw * (currentPosition - focus);
            currentRotation = inverseYaw * currentRotation;
        }

        Mode = newMode;
        activeShot = shot;
        transitionStartPosition = currentPosition;
        transitionStartRotation = currentRotation;
        transitionTime = 0f;
        turntableActive = continueTurntable;
    }

    void EnterOrbit(CameraMode newMode)
    {
        if (target == null)
        {
            Debug.LogWarning(nameof(Lab_CameraDirector) + ": Orbit target is not assigned.", this);
            return;
        }

        SyncOrbitFromCurrentView();
        activeShot = null;
        turntableActive = newMode == CameraMode.Turntable;
        shotTurntableYaw = 0f;
        Mode = newMode;
        skipNextOrbitInputFrame = newMode == CameraMode.Orbit;
        if (newMode == CameraMode.Turntable)
        {
            turntableStartedAt = Time.unscaledTime;
            waitForTurntablePointerRelease = true;
        }
        else
        {
            waitForTurntablePointerRelease = false;
        }
    }

    void SyncOrbitFromCurrentView()
    {
        Vector3 offset = targetCamera.transform.position - FocusPosition;
        float rawDistance = offset.magnitude;

        if (rawDistance < 0.001f)
        {
            offset = targetCamera.transform.rotation * Vector3.back;
            orbitDistance = MinimumDistance;
        }
        else
        {
            orbitDistance = Mathf.Max(rawDistance, MinimumDistance);
        }

        runtimeMaximumDistance = Mathf.Max(MaximumDistance, orbitDistance);

        Vector3 direction = offset.normalized;
        float currentPitch = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
        orbitAngles.x = currentPitch;
        orbitAngles.y = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180f;
        runtimeMinimumPitch = Mathf.Min(MinimumPitch, currentPitch);
        runtimeMaximumPitch = Mathf.Max(MaximumPitch, currentPitch);
    }

    bool HandleOrbitInput()
    {
        mouse ??= Mouse.current;
        if (mouse == null || !IsOrbitClickArea()) return false;

        if (skipNextOrbitInputFrame)
        {
            skipNextOrbitInputFrame = false;
            return false;
        }

        bool changed = false;

        if (mouse.leftButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude > 0.0001f)
            {
                float oldPitch = orbitAngles.x;
                orbitAngles.x = Mathf.Clamp(
                    orbitAngles.x - delta.y * rotationSpeed,
                    runtimeMinimumPitch,
                    runtimeMaximumPitch);
                float pitchDelta = orbitAngles.x - oldPitch;
                float yawDelta = delta.x * rotationSpeed;
                orbitAngles.y += yawDelta;
                ApplyIncrementalOrbit(yawDelta, pitchDelta);
                changed = true;
            }
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newDistance = Mathf.Clamp(
                orbitDistance - scroll * zoomSpeed,
                MinimumDistance,
                runtimeMaximumDistance);
            ApplyIncrementalZoom(newDistance);
            orbitDistance = newDistance;
            changed = true;
        }

        return changed;
    }

    void ApplyIncrementalOrbit(float yawDelta, float pitchDelta)
    {
        Vector3 focus = FocusPosition;
        Transform cameraTransform = targetCamera.transform;
        Vector3 offset = cameraTransform.position - focus;

        if (Mathf.Abs(yawDelta) > 0.0001f)
        {
            Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, Vector3.up);
            offset = yawRotation * offset;
            cameraTransform.rotation = yawRotation * cameraTransform.rotation;
        }

        if (Mathf.Abs(pitchDelta) > 0.0001f)
        {
            Quaternion pitchRotation = Quaternion.AngleAxis(
                pitchDelta,
                cameraTransform.right);
            Vector3 pitchedOffset = pitchRotation * offset;
            bool heightAllowed = !preventOrbitBelowFloor ||
                focus.y + pitchedOffset.y >= minimumOrbitCameraY;

            if (heightAllowed)
            {
                offset = pitchedOffset;
                cameraTransform.rotation = pitchRotation * cameraTransform.rotation;
            }
            else
            {
                orbitAngles.x -= pitchDelta;
            }
        }

        cameraTransform.position = focus + offset;
    }

    void ApplyIncrementalZoom(float newDistance)
    {
        Vector3 focus = FocusPosition;
        Transform cameraTransform = targetCamera.transform;
        Vector3 offset = cameraTransform.position - focus;
        if (offset.sqrMagnitude < 0.000001f) return;

        cameraTransform.position = focus + offset.normalized * newDistance;
    }

    void UpdateOrbit()
    {
        if (target == null) return;

        Vector3 focus = FocusPosition;
        Quaternion orbitRotation = Quaternion.Euler(orbitAngles.x, orbitAngles.y, 0f);
        Vector3 position = focus + orbitRotation * (Vector3.back * orbitDistance);
        Vector3 lookDirection = focus - position;
        if (lookDirection.sqrMagnitude < 0.0001f) return;
        Quaternion cameraRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

        targetCamera.transform.SetPositionAndRotation(position, cameraRotation);
    }

    void HandleKeyboardShortcuts()
    {
        if (!enableKeyboardShortcuts) return;
        keyboard ??= Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.qKey.wasPressedThisFrame) SelectClose();
        if (keyboard.wKey.wasPressedThisFrame) SelectMid();
        if (keyboard.eKey.wasPressedThisFrame) SelectFull();
        if (keyboard.rKey.wasPressedThisFrame) SelectTurntable();
        if (keyboard.tKey.wasPressedThisFrame) SelectOrbit();
    }

    bool PointerPressedThisFrame()
    {
        mouse ??= Mouse.current;
        return mouse != null &&
            (mouse.leftButton.wasPressedThisFrame ||
             mouse.rightButton.wasPressedThisFrame ||
             mouse.middleButton.wasPressedThisFrame);
    }

    bool IsOrbitClickArea()
    {
        mouse ??= Mouse.current;
        if (mouse == null) return false;

        Vector2 pointer = mouse.position.ReadValue();
        float sideMargin = Screen.width * 0.2f;
        return pointer.x >= sideMargin &&
               pointer.x <= Screen.width - sideMargin &&
               pointer.y >= 0f && pointer.y <= Screen.height;
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
}
