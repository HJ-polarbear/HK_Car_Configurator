namespace URPLabStudio
{
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Lab_IntroLogoSequence : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image blackOverlay;
    [SerializeField] private Image unityLogo;
    [SerializeField] private Image varcoLogo;

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip stageMusicClip;

    [Header("Unity Logo Timing")]
    [SerializeField] private float unityFadeInTime = 1.0f;
    [SerializeField] private float unityHoldTime = 1.5f;
    [SerializeField] private float unityFadeOutTime = 1.0f;

    [Header("Varco Logo Timing")]
    [SerializeField] private float varcoFadeInTime = 1.0f;
    [SerializeField] private float varcoHoldTime = 2.0f;
    [SerializeField] private float varcoFadeOutTime = 1.0f;

    [Header("Black Overlay Timing")]
    [SerializeField] private float finalBlackFadeOutTime = 1.5f;

    [Header("Options")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loopBgm = true;
    [SerializeField] private float bgmVolume = 1.0f;

    private void Awake()
    {
        InitializeVisualState();
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartCoroutine(PlaySequence());
        }
    }

    private void InitializeVisualState()
    {
        if (blackOverlay != null)
        {
            SetImageAlpha(blackOverlay, 1f);
        }

        if (unityLogo != null)
        {
            SetImageAlpha(unityLogo, 0f);
        }

        if (varcoLogo != null)
        {
            SetImageAlpha(varcoLogo, 0f);
        }

        if (bgmSource != null)
        {
            bgmSource.playOnAwake = false;
            bgmSource.loop = loopBgm;
            bgmSource.volume = bgmVolume;
        }
    }

    public void PlayIntro()
    {
        StopAllCoroutines();
        InitializeVisualState();
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        if (unityLogo != null)
        {
            yield return FadeImage(unityLogo, 0f, 1f, unityFadeInTime);
            yield return new WaitForSeconds(unityHoldTime);
            yield return FadeImage(unityLogo, 1f, 0f, unityFadeOutTime);
        }

        if (varcoLogo != null)
        {
            yield return FadeImage(varcoLogo, 0f, 1f, varcoFadeInTime);

            PlayStageMusic();

            yield return new WaitForSeconds(varcoHoldTime);
            yield return FadeImage(varcoLogo, 1f, 0f, varcoFadeOutTime);
        }
        else
        {
            PlayStageMusic();
        }

        if (blackOverlay != null)
        {
            yield return FadeImage(blackOverlay, 1f, 0f, finalBlackFadeOutTime);
        }
    }

    private void PlayStageMusic()
    {
        if (bgmSource == null || stageMusicClip == null)
        {
            return;
        }

        if (bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = stageMusicClip;
        bgmSource.loop = loopBgm;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    private IEnumerator FadeImage(Image target, float from, float to, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetImageAlpha(target, to);
            yield break;
        }

        SetImageAlpha(target, from);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);
            SetImageAlpha(target, alpha);
            yield return null;
        }

        SetImageAlpha(target, to);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
}
