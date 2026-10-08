using URPLabStudio;
using System.Collections;
using UnityEngine;

namespace Lab.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WheelCollider))]
    public sealed class Lab_WheelEffects : MonoBehaviour
    {
        [SerializeField] private Transform skidTrailPrefab;
        [SerializeField] private ParticleSystem skidParticles;
        [SerializeField] private AudioSource skidAudio;
        [SerializeField, Min(0.1f)] private float trailLifetime = 10f;

        private WheelCollider wheel;
        private Transform activeTrail;
        private sealed class StaticState { public Transform DetachedTrails; }
        private static readonly StaticState State = new StaticState();

        public bool IsSkidding { get; private set; }
        public bool IsAudioPlaying => skidAudio != null && skidAudio.isPlaying;

        private void Awake()
        {
            wheel = GetComponent<WheelCollider>();
            if (skidAudio == null) skidAudio = GetComponent<AudioSource>();
            if (skidParticles == null) skidParticles = transform.root.GetComponentInChildren<ParticleSystem>(true);
            if (State.DetachedTrails == null) State.DetachedTrails = new GameObject("Lab Detached Skid Trails").transform;
        }

        public void SetAssets(Transform trail, ParticleSystem particles, AudioSource audio)
        {
            skidTrailPrefab = trail;
            skidParticles = particles;
            skidAudio = audio;
        }

        public void SetSkidState(bool active)
        {
            if (active)
            {
                if (skidParticles != null)
                {
                    skidParticles.transform.position = transform.position - transform.up * wheel.radius;
                    skidParticles.Emit(1);
                }
                if (!IsSkidding) StartCoroutine(BeginTrail());
            }
            else
            {
                EndTrail();
                if (skidAudio != null && skidAudio.isPlaying) skidAudio.Stop();
            }
        }

        public void PlaySkidAudio()
        {
            if (skidAudio != null && skidAudio.clip != null && !skidAudio.isPlaying) skidAudio.Play();
        }

        private IEnumerator BeginTrail()
        {
            IsSkidding = true;
            if (skidTrailPrefab == null) yield break;
            activeTrail = Instantiate(skidTrailPrefab, transform);
            activeTrail.localPosition = Vector3.down * wheel.radius;
            yield return null;
        }

        private void EndTrail()
        {
            if (!IsSkidding) return;
            IsSkidding = false;
            if (activeTrail == null) return;
            activeTrail.SetParent(State.DetachedTrails, true);
            Destroy(activeTrail.gameObject, trailLifetime);
            activeTrail = null;
        }
    }
}
