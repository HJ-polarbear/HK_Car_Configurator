namespace URPLabStudio
{
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Lab_OpenElement : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Keyboard Input")]
    [SerializeField]
    private Key keyboardKey = Key.Numpad1;

    [Header("Animator Override")]
    [SerializeField]
    private bool disableAnimatorOnStart = true;

    [Header("Open Settings")]
    [SerializeField]
    private RotationAxis rotationAxis = RotationAxis.Y;

    [SerializeField]
    [Range(-180f, 180f)]
    private float openAngle = 65f;

    [Header("Smooth Settings")]
    [SerializeField]
    [Min(0.01f)]
    private float smoothDuration = 0.5f;

    [SerializeField]
    private AnimationCurve smoothCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Runtime State")]
    [SerializeField]
    private bool startOpened = false;

    [SerializeField]
    private bool debugInput = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private Coroutine rotationCoroutine;
    private bool isOpened;

    public Key KeyboardKey => keyboardKey;
    public bool IsOpened => isOpened;

    private void Awake()
    {
        if (disableAnimatorOnStart &&
            TryGetComponent(out Animator animator))
        {
            animator.enabled = false;
        }

        closedRotation = transform.localRotation;

        RecalculateOpenRotation();

        isOpened = startOpened;

        transform.localRotation =
            isOpened ? openRotation : closedRotation;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        var keyControl = Keyboard.current[keyboardKey];

        if (keyControl == null)
            return;

        if (!keyControl.wasPressedThisFrame)
            return;

        if (debugInput)
        {
            Debug.Log(
                $"[Lab_OpenElement] Key Pressed: {keyboardKey}",
                this
            );
        }

        Toggle();
    }

    public void Toggle()
    {
        SetOpened(!isOpened);
    }

    // Compatibility alias for UI bridges created against the earlier API.
    public void ToggleElement()
    {
        Toggle();
    }

    public void Open()
    {
        SetOpened(true);
    }

    public void Close()
    {
        SetOpened(false);
    }

    public void SetOpened(bool opened)
    {
        isOpened = opened;

        Quaternion targetRotation =
            isOpened ? openRotation : closedRotation;

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }

        rotationCoroutine =
            StartCoroutine(
                RotateSmoothly(targetRotation)
            );
    }

    private IEnumerator RotateSmoothly(
        Quaternion targetRotation)
    {
        Quaternion startRotation =
            transform.localRotation;

        if (smoothDuration <= 0.01f)
        {
            transform.localRotation =
                targetRotation;

            rotationCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < smoothDuration)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime / smoothDuration
                );

            float curveValue =
                smoothCurve.Evaluate(
                    normalizedTime
                );

            transform.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    curveValue
                );

            yield return null;
        }

        transform.localRotation =
            targetRotation;

        rotationCoroutine = null;
    }

    private void RecalculateOpenRotation()
    {
        Vector3 axis =
            GetRotationAxis();

        openRotation =
            closedRotation *
            Quaternion.AngleAxis(
                openAngle,
                axis
            );
    }

    private Vector3 GetRotationAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                return Vector3.right;

            case RotationAxis.Y:
                return Vector3.up;

            case RotationAxis.Z:
                return Vector3.forward;

            default:
                return Vector3.up;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        RecalculateOpenRotation();
    }
#endif
}
}
