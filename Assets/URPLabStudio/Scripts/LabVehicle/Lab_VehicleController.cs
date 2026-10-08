using URPLabStudio;
using UnityEngine;

namespace Lab.Vehicle
{
    public enum LabDriveLayout { FrontWheelDrive, RearWheelDrive, AllWheelDrive }
    public enum LabSpeedUnit { KilometresPerHour, MilesPerHour }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Lab_VehicleController : MonoBehaviour
    {
        [Header("Wheel Setup: FL, FR, RL, RR")]
        [SerializeField] private WheelCollider[] wheelColliders = new WheelCollider[4];
        [SerializeField] private Transform[] wheelMeshes = new Transform[4];
        [SerializeField] private Lab_WheelEffects[] wheelEffects = new Lab_WheelEffects[4];
        [SerializeField] private LabDriveLayout driveLayout = LabDriveLayout.AllWheelDrive;

        [Header("Chassis")]
        [SerializeField] private Vector3 centreOfMassOffset;
        [SerializeField, Min(0f)] private float downforce = 100f;
        [SerializeField, Range(0f, 1f)] private float directionalGrip = 0.5f;

        [Header("Steering and Power")]
        [SerializeField, Range(1f, 60f)] private float maximumSteerAngle = 25f;
        [SerializeField, Min(0f)] private float driveTorque = 2000f;
        [SerializeField, Min(0f)] private float reverseTorque = 500f;
        [SerializeField, Min(0f)] private float serviceBrakeTorque = 2000f;
        [SerializeField, Min(0f)] private float handbrakeTorque = 6000f;
        [SerializeField, Range(0f, 1f)] private float tractionControl = 0.5f;
        [SerializeField, Min(0.01f)] private float slipLimit = 0.3f;

        [Header("Speed")]
        [SerializeField] private LabSpeedUnit speedUnit = LabSpeedUnit.KilometresPerHour;
        [SerializeField, Min(1f)] private float topSpeed = 200f;

        private Rigidbody body;
        private float throttle;
        private float brake;
        private float steering;
        private float handbrake;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float NormalizedEngineSpeed { get; private set; }
        public float ThrottleInput => throttle;
        public float BrakeInput => brake;
        public bool IsSkidding { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = centreOfMassOffset;
        }

        private void FixedUpdate()
        {
            ApplySteering();
            ApplyDriveAndBrakes();
            ApplyDirectionalGrip();
            ApplyDownforce();
            LimitTopSpeed();
            UpdateWheelEffects();
            UpdateEngineSpeed();
        }

        private void LateUpdate()
        {
            int count = Mathf.Min(wheelColliders.Length, wheelMeshes.Length);
            for (int i = 0; i < count; i++)
            {
                if (wheelColliders[i] == null || wheelMeshes[i] == null) continue;
                wheelColliders[i].GetWorldPose(out Vector3 position, out Quaternion rotation);
                wheelMeshes[i].SetPositionAndRotation(position, rotation);
            }
        }

        public void SetInput(float steer, float acceleration, float braking, float parkingBrake)
        {
            steering = Mathf.Clamp(steer, -1f, 1f);
            throttle = Mathf.Clamp01(acceleration);
            brake = Mathf.Clamp01(braking);
            handbrake = Mathf.Clamp01(parkingBrake);
        }

        public void Configure(WheelCollider[] colliders, Transform[] meshes, Lab_WheelEffects[] effects,
            Vector3 centre, float steerAngle, float motorTorque, float reverse, float parkingBrake,
            float aeroDownforce, float maxSpeed, float skidThreshold, float brakeTorque)
        {
            wheelColliders = colliders; wheelMeshes = meshes; wheelEffects = effects;
            centreOfMassOffset = centre; maximumSteerAngle = steerAngle; driveTorque = motorTorque;
            reverseTorque = reverse; handbrakeTorque = parkingBrake; downforce = aeroDownforce;
            topSpeed = maxSpeed; slipLimit = skidThreshold; serviceBrakeTorque = brakeTorque;
        }

        private void ApplySteering()
        {
            float angle = steering * maximumSteerAngle;
            if (wheelColliders.Length > 0 && wheelColliders[0] != null) wheelColliders[0].steerAngle = angle;
            if (wheelColliders.Length > 1 && wheelColliders[1] != null) wheelColliders[1].steerAngle = angle;
        }

        private void ApplyDriveAndBrakes()
        {
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            bool reversing = brake > 0f && forwardSpeed < 1.5f;
            int drivenCount = driveLayout == LabDriveLayout.AllWheelDrive ? 4 : 2;
            float motor = (reversing ? -reverseTorque * brake : driveTorque * throttle) / drivenCount;

            for (int i = 0; i < wheelColliders.Length; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                if (wheel == null) continue;
                bool driven = driveLayout == LabDriveLayout.AllWheelDrive ||
                              (driveLayout == LabDriveLayout.FrontWheelDrive && i < 2) ||
                              (driveLayout == LabDriveLayout.RearWheelDrive && i >= 2);
                wheel.motorTorque = driven ? ApplyTractionLimit(wheel, motor) : 0f;
                wheel.brakeTorque = reversing ? 0f : brake * serviceBrakeTorque;
                if (i >= 2) wheel.brakeTorque = Mathf.Max(wheel.brakeTorque, handbrake * handbrakeTorque);
            }
        }

        private float ApplyTractionLimit(WheelCollider wheel, float requestedTorque)
        {
            if (!wheel.GetGroundHit(out WheelHit hit)) return 0f;
            float excessSlip = Mathf.Max(0f, Mathf.Abs(hit.forwardSlip) - slipLimit);
            return requestedTorque * Mathf.Clamp01(1f - excessSlip * tractionControl);
        }

        private void ApplyDirectionalGrip()
        {
            if (body.linearVelocity.sqrMagnitude < 1f) return;
            Vector3 local = transform.InverseTransformDirection(body.linearVelocity);
            local.x = Mathf.Lerp(local.x, 0f, directionalGrip * Time.fixedDeltaTime * 3f);
            body.linearVelocity = transform.TransformDirection(local);
        }

        private void ApplyDownforce() => body.AddForce(-transform.up * downforce * body.linearVelocity.magnitude);

        private void LimitTopSpeed()
        {
            float multiplier = speedUnit == LabSpeedUnit.KilometresPerHour ? 3.6f : 2.2369363f;
            float current = body.linearVelocity.magnitude * multiplier;
            if (current > topSpeed) body.linearVelocity = body.linearVelocity.normalized * (topSpeed / multiplier);
        }

        private void UpdateWheelEffects()
        {
            IsSkidding = false;
            bool audioClaimed = false;
            for (int i = 0; i < wheelColliders.Length; i++)
            {
                if (wheelColliders[i] == null) continue;
                bool slipping = wheelColliders[i].GetGroundHit(out WheelHit hit) &&
                                (Mathf.Abs(hit.forwardSlip) >= slipLimit || Mathf.Abs(hit.sidewaysSlip) >= slipLimit);
                IsSkidding |= slipping;
                if (i >= wheelEffects.Length || wheelEffects[i] == null) continue;
                wheelEffects[i].SetSkidState(slipping);
                if (slipping && !audioClaimed) { wheelEffects[i].PlaySkidAudio(); audioClaimed = true; }
            }
        }

        private void UpdateEngineSpeed()
        {
            float wheelRpm = 0f; int count = 0;
            foreach (WheelCollider wheel in wheelColliders)
                if (wheel != null) { wheelRpm += Mathf.Abs(wheel.rpm); count++; }
            float rpmFactor = count == 0 ? 0f : wheelRpm / count / 1500f;
            NormalizedEngineSpeed = Mathf.Clamp01(Mathf.Max(rpmFactor, throttle * 0.35f));
        }
    }
}
