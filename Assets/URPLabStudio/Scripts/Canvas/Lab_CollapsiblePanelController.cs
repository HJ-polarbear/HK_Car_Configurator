namespace URPLabStudio
{
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Lab_CollapsiblePanelController : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text arrowLabel;
    [SerializeField] private Vector2 openPosition;
    [SerializeField] private Vector2 closedPosition;
    [SerializeField] private string openArrow = "<";
    [SerializeField] private string closedArrow = ">";
    [SerializeField, Min(0.05f)] private float animationDuration = 0.28f;
    [SerializeField] private bool startOpen = true;

    private bool isOpen;
    private Coroutine routine;

    private void Awake()
    {
        isOpen = startOpen;
        if (toggleButton != null)
        {
            if (toggleButton.targetGraphic != null) toggleButton.targetGraphic.raycastTarget = true;
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(TogglePanel);
        }
        ApplyImmediate();
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        if (panel == null) yield break;
        Vector2 from = panel.anchoredPosition;
        Vector2 to = isOpen ? openPosition : closedPosition;
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = t * t * (3f - 2f * t);
            panel.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }
        panel.anchoredPosition = to;
        UpdateArrow();
        routine = null;
    }

    private void ApplyImmediate()
    {
        if (panel != null) panel.anchoredPosition = isOpen ? openPosition : closedPosition;
        UpdateArrow();
    }

    private void UpdateArrow()
    {
        if (arrowLabel != null) arrowLabel.text = isOpen ? openArrow : closedArrow;
    }
}
}
