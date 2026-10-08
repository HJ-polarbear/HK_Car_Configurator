namespace URPLabStudio
{
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Lab_StageEndFade : MonoBehaviour
{
    [Header("Fade Overlay")]
    [SerializeField]
    private Image blackOverlay;

    [Header("Music Detection")]
    [SerializeField]
    private string targetClipName = "Stephani B - Blade";

    [SerializeField, Min(0.05f)]
    private float audioSearchInterval = 0.25f;

    [Header("Ending Timing")]
    [SerializeField, Min(0f)]
    private float fadeStartBeforeMusicEnd = 5.0f;

    [SerializeField, Min(0.01f)]
    private float fadeDuration = 4.0f;

    [SerializeField, Min(0f)]
    private float blackHoldBeforeMusicEnd = 1.0f;

    [Header("Audio Options")]
    [SerializeField]
    private bool disableMusicLoop = true;

    [SerializeField]
    private bool fadeAudioVolume = false;

    [SerializeField, Min(0.01f)]
    private float audioFadeDuration = 3.0f;

    [Header("Playback Options")]
    [SerializeField]
    private bool playAutomatically = true;

    [SerializeField]
    private bool useUnscaledTime = true;

    [SerializeField]

    private AudioSource targetAudioSource;
    private Coroutine monitorCoroutine;
    private Coroutine visualFadeCoroutine;
    private Coroutine audioFadeCoroutine;

    private bool fadeTriggered;
    private float originalAudioVolume = 1.0f;

    private void OnEnable()
    {
        if (playAutomatically)
        {
            BeginMonitoring();
        }
    }

    private void OnDisable()
    {
        StopAllRunningCoroutines();
    }

    public void BeginMonitoring()
    {
        StopAllRunningCoroutines();

        fadeTriggered = false;
        targetAudioSource = null;

        monitorCoroutine = StartCoroutine(MonitorMusicRoutine());
    }

    public void StopMonitoring()
    {
        if (monitorCoroutine != null)
        {
            StopCoroutine(monitorCoroutine);
            monitorCoroutine = null;
        }
    }

    public void SetTargetAudioSource(AudioSource audioSource)
    {
        targetAudioSource = audioSource;

        if (targetAudioSource == null)
        {
            return;
        }

        originalAudioVolume = targetAudioSource.volume;

        if (disableMusicLoop)
        {
            targetAudioSource.loop = false;
        }
    }

    private IEnumerator MonitorMusicRoutine()
    {
        if (blackOverlay == null)
        {
            Debug.LogError(
                $"{nameof(Lab_StageEndFade)}: Black Overlay is not assigned.",
                this);

            monitorCoroutine = null;
            yield break;
        }

        blackOverlay.gameObject.SetActive(true);
        blackOverlay.raycastTarget = false;

        while (targetAudioSource == null)
        {
            targetAudioSource = FindTargetAudioSource();

            if (targetAudioSource == null)
            {
                yield return WaitForSecondsSafe(audioSearchInterval);
            }
        }

        originalAudioVolume = targetAudioSource.volume;

        if (disableMusicLoop)
        {
            targetAudioSource.loop = false;
        }

        while (targetAudioSource != null &&
               !targetAudioSource.isPlaying)
        {
            yield return null;
        }

        if (targetAudioSource == null ||
            targetAudioSource.clip == null)
        {
            monitorCoroutine = null;
            yield break;
        }

        while (!fadeTriggered)
        {
            if (targetAudioSource == null ||
                targetAudioSource.clip == null)
            {
                monitorCoroutine = null;
                yield break;
            }

            float remainingTime =
                GetRemainingMusicTime(targetAudioSource);

            float minimumRequiredTime =
                fadeDuration + blackHoldBeforeMusicEnd;

            float triggerTime = Mathf.Max(
                fadeStartBeforeMusicEnd,
                minimumRequiredTime);

            if (remainingTime <= triggerTime)
            {
                fadeTriggered = true;

                visualFadeCoroutine =
                    StartCoroutine(FadeBlackOverlayRoutine());

                if (fadeAudioVolume)
                {
                    audioFadeCoroutine =
                        StartCoroutine(FadeAudioRoutine());
                }

                break;
            }

            yield return null;
        }

        monitorCoroutine = null;
    }

    private AudioSource FindTargetAudioSource()
    {
        AudioSource[] audioSources =
            FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include);

        AudioSource fallbackSource = null;

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];

            if (source == null ||
                source.clip == null)
            {
                continue;
            }

            if (string.Equals(
                    source.clip.name,
                    targetClipName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }

            if (source.clip.name.IndexOf(
                    targetClipName,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fallbackSource = source;
            }
        }

        return fallbackSource;
    }

    private float GetRemainingMusicTime(AudioSource source)
    {
        if (source == null ||
            source.clip == null)
        {
            return float.MaxValue;
        }

        AudioClip clip = source.clip;

        if (clip.frequency > 0 &&
            source.timeSamples >= 0)
        {
            int remainingSamples =
                Mathf.Max(
                    0,
                    clip.samples - source.timeSamples);

            return (float)remainingSamples /
                   clip.frequency;
        }

        return Mathf.Max(
            0f,
            clip.length - source.time);
    }

    private IEnumerator FadeBlackOverlayRoutine()
    {
        if (blackOverlay == null)
        {
            visualFadeCoroutine = null;
            yield break;
        }

        blackOverlay.gameObject.SetActive(true);
        blackOverlay.transform.SetAsLastSibling();

        Color color = blackOverlay.color;
        float startAlpha = color.a;

        if (fadeDuration <= 0f)
        {
            color.a = 1f;
            blackOverlay.color = color;

            visualFadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += GetDeltaTime();

            float normalizedTime =
                Mathf.Clamp01(elapsed / fadeDuration);

            float smoothTime =
                normalizedTime *
                normalizedTime *
                (3f - 2f * normalizedTime);

            color.a = Mathf.Lerp(
                startAlpha,
                1f,
                smoothTime);

            blackOverlay.color = color;

            yield return null;
        }

        color.a = 1f;
        blackOverlay.color = color;

        visualFadeCoroutine = null;
    }

    private IEnumerator FadeAudioRoutine()
    {
        if (targetAudioSource == null)
        {
            audioFadeCoroutine = null;
            yield break;
        }

        float startVolume =
            targetAudioSource.volume;

        if (audioFadeDuration <= 0f)
        {
            targetAudioSource.volume = 0f;

            audioFadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < audioFadeDuration)
        {
            if (targetAudioSource == null)
            {
                audioFadeCoroutine = null;
                yield break;
            }

            elapsed += GetDeltaTime();

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / audioFadeDuration);

            targetAudioSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0f,
                    normalizedTime);

            yield return null;
        }

        if (targetAudioSource != null)
        {
            targetAudioSource.volume = 0f;
        }

        audioFadeCoroutine = null;
    }

    private object WaitForSecondsSafe(float duration)
    {
        if (useUnscaledTime)
        {
            return new WaitForSecondsRealtime(duration);
        }

        return new WaitForSeconds(duration);
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }

    public void ResetEndingFade()
    {
        StopAllRunningCoroutines();

        fadeTriggered = false;

        if (blackOverlay != null)
        {
            blackOverlay.gameObject.SetActive(true);

            Color color = blackOverlay.color;
            color.a = 0f;
            blackOverlay.color = color;
        }

        if (targetAudioSource != null)
        {
            targetAudioSource.volume =
                originalAudioVolume;
        }

        monitorCoroutine =
            StartCoroutine(MonitorMusicRoutine());
    }

    private void StopAllRunningCoroutines()
    {
        if (monitorCoroutine != null)
        {
            StopCoroutine(monitorCoroutine);
            monitorCoroutine = null;
        }

        if (visualFadeCoroutine != null)
        {
            StopCoroutine(visualFadeCoroutine);
            visualFadeCoroutine = null;
        }

        if (audioFadeCoroutine != null)
        {
            StopCoroutine(audioFadeCoroutine);
            audioFadeCoroutine = null;
        }
    }
}
}
