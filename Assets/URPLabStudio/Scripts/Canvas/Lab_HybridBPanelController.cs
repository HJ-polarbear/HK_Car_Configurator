namespace URPLabStudio
{
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Lab_HybridBPanelController : MonoBehaviour
{
    [SerializeField] private RectTransform slidingPanel;
    [SerializeField] private TMP_Text arrowLabel;
    [SerializeField] private float closedX = -274f;
    [SerializeField, Min(0.05f)] private float animationDuration = 0.28f;
    [SerializeField] private bool startOpen = true;

    private Coroutine animationRoutine;
    private bool isOpen;

    private void Awake()
    {
        Button tabButton = slidingPanel != null ? slidingPanel.GetComponentInChildren<Button>(true) : null;
        if (tabButton != null && tabButton.targetGraphic != null)
        {
            tabButton.targetGraphic.raycastTarget = true;
        }

        isOpen = startOpen;
        ApplyImmediate();
    }

    public void TogglePanel()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(AnimatePanel());
    }

    private IEnumerator AnimatePanel()
    {
        if (slidingPanel == null)
        {
            yield break;
        }

        Vector2 start = slidingPanel.anchoredPosition;
        Vector2 target = new Vector2(isOpen ? 0f : closedX, start.y);
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = t * t * (3f - 2f * t);
            slidingPanel.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        slidingPanel.anchoredPosition = target;
        UpdateArrow();
        animationRoutine = null;
    }

    private void ApplyImmediate()
    {
        if (slidingPanel != null)
        {
            Vector2 position = slidingPanel.anchoredPosition;
            position.x = isOpen ? 0f : closedX;
            slidingPanel.anchoredPosition = position;
        }

        UpdateArrow();
    }

    private void UpdateArrow()
    {
        if (arrowLabel != null)
        {
            arrowLabel.text = isOpen ? "<" : ">";
        }
    }
}
}
