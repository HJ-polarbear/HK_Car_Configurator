namespace URPLabStudio
{
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Lab_OrbitControlSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum SlotRole { Category, Action }
    public enum ActionType { Wheel, Paint, Camera, Door, Lights, Explode, Custom }

    [Header("Slot")]
    [SerializeField] SlotRole role;
    [SerializeField] Lab_OrbitPanelController owner;

    [Header("Category")]
    [SerializeField] string categoryId;
    [SerializeField] Renderer[] tintRenderers = Array.Empty<Renderer>();
    [SerializeField] GameObject idleVisual;
    [SerializeField] GameObject activeVisual;

    [Header("Action")]
    [SerializeField] ActionType actionType;
    [SerializeField] int optionIndex;
    [SerializeField] Color paintColor = Color.white;
    [SerializeField] Image selectionBar;
    [SerializeField] UnityEvent customAction;

    bool actionSelected;

    public bool IsCategory => role == SlotRole.Category;
    public string CategoryId => categoryId;
    public ActionType Action => actionType;
    public int OptionIndex => optionIndex;
    public Color PaintColor => paintColor;
    public Lab_OrbitPanelController Owner => owner;

    void OnEnable()
    {
        owner?.Register(this);
    }

    void OnDisable()
    {
        owner?.Unregister(this);
    }

    public void AssignOwner(Lab_OrbitPanelController controller)
    {
        if (owner == controller)
            return;
        owner?.Unregister(this);
        owner = controller;
        if (isActiveAndEnabled)
            owner?.Register(this);
    }

    public void ConfigureCategory(Lab_OrbitPanelController controller, string id)
    {
        role = SlotRole.Category;
        categoryId = string.IsNullOrWhiteSpace(id) ? name.ToUpperInvariant() : id.ToUpperInvariant();
        AssignOwner(controller);
        AutoConfigureVisuals();
    }

    public void ConfigureAction(Lab_OrbitPanelController controller, ActionType type, int index,
        Color color, Image bar)
    {
        role = SlotRole.Action;
        actionType = type;
        optionIndex = index;
        paintColor = color;
        selectionBar = bar;
        AssignOwner(controller);
    }

    public void AutoConfigureVisuals()
    {
        tintRenderers = GetComponentsInChildren<Renderer>(true);
        idleVisual = null;
        activeVisual = null;
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            string upper = child.name.ToUpperInvariant();
            if (upper.EndsWith("_GRAY", StringComparison.Ordinal))
                idleVisual = child.gameObject;
            else if (upper.EndsWith("_ORANGE", StringComparison.Ordinal))
                activeVisual = child.gameObject;
        }

        if (idleVisual == null || activeVisual == null)
        {
            Transform searchRoot = transform.parent;
            if (searchRoot == null)
                return;
            string categoryToken = string.IsNullOrWhiteSpace(categoryId) ? name : categoryId;
            foreach (Transform candidate in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                string upper = candidate.name.ToUpperInvariant();
                if (upper.IndexOf(categoryToken, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (idleVisual == null && upper.EndsWith("_GRAY", StringComparison.Ordinal))
                    idleVisual = candidate.gameObject;
                if (activeVisual == null && upper.EndsWith("_ORANGE", StringComparison.Ordinal))
                    activeVisual = candidate.gameObject;
            }
        }
    }

    public void SetCategoryState(bool hovered, bool selected, Color selectedTint)
    {
        if (!IsCategory)
            return;
        bool active = hovered || selected;
        if (idleVisual != null)
            idleVisual.SetActive(!active);
        if (activeVisual != null)
            activeVisual.SetActive(active);
        Color tint = active ? selectedTint : Color.white;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (Renderer renderer in tintRenderers)
        {
            if (renderer == null)
                continue;
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
        }
    }

    public void SetActionSelected(bool value)
    {
        if (IsCategory)
            return;
        actionSelected = value;
        if (selectionBar != null)
            selectionBar.color = value
                ? new Color(.94f, .25f, .06f, 1f)
                : new Color(.72f, .73f, .75f, .35f);
        transform.localScale = value ? Vector3.one * 1.06f : Vector3.one;
    }

    public void InvokeCustomAction()
    {
        customAction?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsCategory)
            owner?.SetHovered(this);
        else if (!actionSelected)
            transform.localScale = Vector3.one * 1.035f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsCategory)
            owner?.ClearHovered(this);
        else
            transform.localScale = actionSelected ? Vector3.one * 1.06f : Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsCategory)
            owner?.SelectCategory(this);
        else
            owner?.InvokeAction(this);
    }
}
}
