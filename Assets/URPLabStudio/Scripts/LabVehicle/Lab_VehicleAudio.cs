using URPLabStudio;
using UnityEngine;

namespace Lab.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Lab_VehicleController))]
    public sealed class Lab_VehicleAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip accelerationLow;
        [SerializeField] private AudioClip accelerationHigh;
        [SerializeField] private AudioClip decelerationHigh;
        [SerializeField, Range(0f, 2f)] private float volume = 0.75f;
        [SerializeField] private float maxDistance = 120f;

        private Lab_VehicleController vehicle;
        private AudioSource low;
        private AudioSource high;
        private AudioSource coast;

        private void Awake()
        {
            vehicle = GetComponent<Lab_VehicleController>();
            low = CreateSource(accelerationLow);
            high = CreateSource(accelerationHigh);
            coast = CreateSource(decelerationHigh);
        }

        public void Configure(AudioClip lowClip, AudioClip highClip, AudioClip coastClip, float rolloff)
        { accelerationLow = lowClip; accelerationHigh = highClip; decelerationHigh = coastClip; maxDistance = rolloff; }

        private AudioSource CreateSource(AudioClip clip)
        {
            if (clip == null) return null;
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip; source.loop = true; source.playOnAwake = false;
            source.spatialBlend = 1f; source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3f; source.maxDistance = maxDistance; source.dopplerLevel = 0.35f;
            source.Play(); return source;
        }

        private void Update()
        {
            float rpm = vehicle.NormalizedEngineSpeed;
            float load = Mathf.Clamp01(vehicle.ThrottleInput);
            Set(low, Mathf.Lerp(0.75f, 1.45f, rpm), volume * (1f - rpm * 0.65f) * (0.35f + load * 0.65f));
            Set(high, Mathf.Lerp(0.7f, 1.35f, rpm), volume * rpm * load);
            Set(coast, Mathf.Lerp(0.65f, 1.2f, rpm), volume * rpm * (1f - load) * 0.65f);
        }

        private static void Set(AudioSource source, float pitch, float level)
        { if (source != null) { source.pitch = pitch; source.volume = level; } }
    }
}
