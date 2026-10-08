namespace URPLabStudio
{
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class Lab_WheelButtonProxy : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    [SerializeField] private Lab_WheelSelection controller;
    [SerializeField, Range(0, 3)] private int index;

    public void Configure(Lab_WheelSelection target, int wheelIndex)
    {
        controller = target;
        index = Mathf.Clamp(wheelIndex, 0, 3);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && controller != null)
            controller.Select(index);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (controller != null) controller.Select(index);
    }
}
}
