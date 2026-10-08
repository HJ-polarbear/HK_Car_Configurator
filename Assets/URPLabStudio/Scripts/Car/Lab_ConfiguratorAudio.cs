namespace URPLabStudio
{
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class Lab_ConfiguratorAudio : MonoBehaviour
{
    public static Lab_ConfiguratorAudio Instance => FindAnyObjectByType<Lab_ConfiguratorAudio>();

    [SerializeField] private AudioClip colorChange;
    [SerializeField] private AudioClip explode;
    [SerializeField] private AudioClip lightOpen;
    [SerializeField] private AudioClip lightClose;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    public void PlayColorChange() => Play(colorChange);
    public void PlayExplode() => Play(explode);
    public void PlayLights(bool on) => Play(on ? lightOpen : lightClose);

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        if (source == null) source = GetComponent<AudioSource>();
        source.PlayOneShot(clip, volume);
    }
}
}
