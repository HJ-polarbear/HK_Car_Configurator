using System.Collections;
using UnityEngine;

namespace URPLabStudio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class Lab_MainIntroController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float logoDisplayDuration = 1.5f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 1f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool disableAfterFade = true;
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine sequence;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            SetAlpha(1f);
        }

        private void Start()
        {
            if (playOnStart) PlayIntro();
        }

        public void PlayIntro()
        {
            if (sequence != null) StopCoroutine(sequence);
            gameObject.SetActive(true);
            SetAlpha(1f);
            sequence = StartCoroutine(PlayRoutine());
        }

        public void SkipIntro()
        {
            if (sequence != null) StopCoroutine(sequence);
            CompleteIntro();
        }

        private IEnumerator PlayRoutine()
        {
            if (logoDisplayDuration > 0f) yield return Wait(logoDisplayDuration);

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += DeltaTime;
                SetAlpha(1f - Mathf.Clamp01(elapsed / fadeOutDuration));
                yield return null;
            }

            CompleteIntro();
        }

        private void CompleteIntro()
        {
            sequence = null;
            SetAlpha(0f);
            if (disableAfterFade) gameObject.SetActive(false);
        }

        private object Wait(float duration) => useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);
        private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private void SetAlpha(float alpha)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = Mathf.Clamp01(alpha);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = canvasGroup.alpha > 0f;
        }
    }
}
