using URPLabStudio;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Lab.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class Lab_CameraSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject[] cameras = System.Array.Empty<GameObject>();
        [SerializeField] private TMP_Text label;
        private int index;
        private InputAction cycleAction;

        public void Configure(GameObject[] items, TMP_Text text) { cameras = items; label = text; Sync(); }
        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cycleAction = new InputAction("Cycle Camera", InputActionType.Button, "<Keyboard>/c");
            cycleAction.AddBinding("<Gamepad>/select");
            cycleAction.performed += OnCycle;
            cycleAction.Enable();
            Sync();
        }
        private void OnDisable()
        {
            if (cycleAction == null) return;
            cycleAction.performed -= OnCycle;
            cycleAction.Disable();
            cycleAction.Dispose();
            cycleAction = null;
        }
        private void OnCycle(InputAction.CallbackContext context) => NextCamera();
        public void NextCamera()
        {
            if (cameras == null || cameras.Length == 0) return;
            int start = index;
            do { index = (index + 1) % cameras.Length; } while (cameras[index] == null && index != start);
            for (int i = 0; i < cameras.Length; i++) if (cameras[i] != null) cameras[i].SetActive(i == index);
            UpdateLabel();
        }
        private void Sync()
        {
            if (cameras == null || cameras.Length == 0) return;
            for (int i = 0; i < cameras.Length; i++) if (cameras[i] != null && cameras[i].activeSelf) { index = i; break; }
            UpdateLabel();
        }
        private void UpdateLabel() { if (label != null && cameras != null && cameras.Length > index && cameras[index] != null) label.text = cameras[index].name; }
    }
}
