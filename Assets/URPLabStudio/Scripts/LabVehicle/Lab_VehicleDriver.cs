using URPLabStudio;
using UnityEngine;

namespace Lab.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Lab_VehicleController))]
    public sealed class Lab_VehicleDriver : MonoBehaviour
    {
        private Lab_VehicleController controller;
        private void Awake() => controller = GetComponent<Lab_VehicleController>();
        private void Update()
        {
            Vector2 drive = Lab_VehicleInput.Drive;
            float throttle = Mathf.Max(0f, drive.y);
            float brake = Mathf.Max(0f, -drive.y);
            controller.SetInput(drive.x, throttle, brake, Lab_VehicleInput.Handbrake ? 1f : 0f);
        }
    }
}
