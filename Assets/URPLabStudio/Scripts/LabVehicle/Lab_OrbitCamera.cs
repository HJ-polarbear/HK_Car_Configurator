using URPLabStudio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class Lab_OrbitCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private float focusHeight = 0.8f;

        [Header("Orbit")]
        [SerializeField] private float rotationSensitivity = 0.15f;
        [SerializeField, Range(1f, 45f)] private float minimumPitch = 8f;
        [SerializeField, Range(45f, 89f)] private float maximumPitch = 75f;

        [Header("Zoom")]
        [SerializeField] private float minimumDistance = 2.5f;
        [SerializeField] private float maximumDistance = 14f;
        [SerializeField] private float zoomSensitivity = 0.01f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.06f;

        private float yaw;
        private float pitch;
        private float distance;
        private Vector3 positionVelocity;
        private bool hasReceivedInput;
        private Quaternion rotationOffset = Quaternion.identity;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                InitializeFromCurrentTransform();
            }
        }

        private void Awake()
        {
            if (target == null)
            {
                GameObject focus = GameObject.Find("CameraFocusPoint");
                if (focus != null)
                    target = focus.transform;
            }
            InitializeFromCurrentTransform();
        }

        private void OnValidate()
        {
            maximumDistance = Mathf.Max(maximumDistance, minimumDistance + 0.1f);
            maximumPitch = Mathf.Max(maximumPitch, minimumPitch);
        }

        private void LateUpdate()
        {
            if (target == null || Mouse.current == null)
                return;

            Mouse mouse = Mouse.current;
            bool isRotating = mouse.rightButton.isPressed;
            float scroll = mouse.scroll.ReadValue().y;

            // Keep the authored scene framing until the player actually uses the orbit controls.
            if (!hasReceivedInput)
            {
                if (!isRotating && Mathf.Approximately(scroll, 0f))
                    return;

                hasReceivedInput = true;
                InitializeFromCurrentTransform();
            }

            if (isRotating)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * rotationSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * rotationSensitivity, minimumPitch, maximumPitch);
            }

            if (!Mathf.Approximately(scroll, 0f))
                distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minimumDistance, maximumDistance);

            Vector3 focusPosition = target.position + Vector3.up * focusHeight;
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = focusPosition + orbitRotation * (Vector3.back * distance);

            transform.position = positionSmoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime)
                : desiredPosition;
            Quaternion orbitLookRotation = Quaternion.LookRotation(focusPosition - transform.position, Vector3.up);
            transform.rotation = orbitLookRotation * rotationOffset;
        }

        private void InitializeFromCurrentTransform()
        {
            if (target == null)
                return;

            Vector3 focusPosition = target.position + Vector3.up * focusHeight;
            Vector3 offset = transform.position - focusPosition;
            distance = Mathf.Clamp(offset.magnitude, minimumDistance, maximumDistance);
            if (offset.sqrMagnitude < 0.0001f)
                offset = Vector3.back;

            Quaternion lookRotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
            yaw = lookRotation.eulerAngles.y;
            pitch = NormalizeAngle(lookRotation.eulerAngles.x);
            rotationOffset = Quaternion.Inverse(lookRotation) * transform.rotation;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
