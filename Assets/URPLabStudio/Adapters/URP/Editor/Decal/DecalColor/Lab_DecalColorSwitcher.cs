namespace URPLabStudio
{
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Lab_DecalColorSwitcher : MonoBehaviour
{
    public enum DecalSlot
    {
        Slot_0,
        Slot_1,
        Slot_2,
        Slot_3,
        Slot_4
    }

    [Header("Room Decal Slots")]
    [SerializeField] private GameObject slot0Root;
    [SerializeField] private GameObject slot1Root;
    [SerializeField] private GameObject slot2Root;
    [SerializeField] private GameObject slot3Root;
    [SerializeField] private GameObject slot4Root;

    [Header("Door Decal Switchers")]
    [SerializeField] private Lab_DoorDecalColorSwitcher[] doorSwitchers;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Startup")]
    [SerializeField] private DecalSlot defaultSlot = DecalSlot.Slot_0;
    [SerializeField] private bool applyOnStart = true;

    private DecalSlot currentSlot;
    private Coroutine transitionCoroutine;

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyInstant(defaultSlot);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.numpad1Key.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame)
        {
            SetSlot(DecalSlot.Slot_0);
        }
        else if (keyboard.numpad2Key.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame)
        {
            SetSlot(DecalSlot.Slot_1);
        }
        else if (keyboard.numpad3Key.wasPressedThisFrame || keyboard.digit3Key.wasPressedThisFrame)
        {
            SetSlot(DecalSlot.Slot_2);
        }
        else if (keyboard.numpad4Key.wasPressedThisFrame || keyboard.digit4Key.wasPressedThisFrame)
        {
            SetSlot(DecalSlot.Slot_3);
        }
        else if (keyboard.numpad5Key.wasPressedThisFrame || keyboard.digit5Key.wasPressedThisFrame)
        {
            SetSlot(DecalSlot.Slot_4);
        }
    }

    public void SetSlot(DecalSlot slot)
    {
        if (!Application.isPlaying || fadeDuration <= 0f)
        {
            ApplyInstant(slot);
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(FadeToSlot(slot));
    }

    private IEnumerator FadeToSlot(DecalSlot newSlot)
    {
        GameObject previousRoot = GetRoot(currentSlot);
        GameObject nextRoot = GetRoot(newSlot);

        ApplyDoorSlot(newSlot, fadeDuration);

        if (previousRoot == nextRoot)
        {
            if (nextRoot != null)
            {
                nextRoot.SetActive(true);
                SetFadeInstant(nextRoot, 1f);
            }

            currentSlot = newSlot;
            transitionCoroutine = null;
            yield break;
        }

        if (nextRoot != null)
        {
            nextRoot.SetActive(true);
            SetFadeInstant(nextRoot, 0f);
        }

        if (previousRoot != null)
        {
            previousRoot.SetActive(true);
            SetFadeInstant(previousRoot, 1f);
        }

        float safeDuration = Mathf.Max(0.01f, fadeDuration);
        float time = 0f;

        while (time < safeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / safeDuration);

            if (previousRoot != null)
            {
                SetFadeInstant(previousRoot, 1f - t);
            }

            if (nextRoot != null)
            {
                SetFadeInstant(nextRoot, t);
            }

            yield return null;
        }

        if (previousRoot != null)
        {
            SetFadeInstant(previousRoot, 0f);
            previousRoot.SetActive(false);
        }

        if (nextRoot != null)
        {
            SetFadeInstant(nextRoot, 1f);
            nextRoot.SetActive(true);
        }

        currentSlot = newSlot;
        transitionCoroutine = null;
    }

    private void ApplyInstant(DecalSlot slot)
    {
        currentSlot = slot;

        ApplyRootInstant(slot0Root, slot == DecalSlot.Slot_0);
        ApplyRootInstant(slot1Root, slot == DecalSlot.Slot_1);
        ApplyRootInstant(slot2Root, slot == DecalSlot.Slot_2);
        ApplyRootInstant(slot3Root, slot == DecalSlot.Slot_3);
        ApplyRootInstant(slot4Root, slot == DecalSlot.Slot_4);

        ApplyDoorSlotInstant(slot);
    }

    private void ApplyRootInstant(GameObject root, bool active)
    {
        if (root == null)
        {
            return;
        }

        root.SetActive(active);
        SetFadeInstant(root, active ? 1f : 0f);
    }

    private void SetFadeInstant(GameObject root, float value)
    {
        if (root == null)
        {
            return;
        }

        DecalProjector[] projectors = root.GetComponentsInChildren<DecalProjector>(true);

        for (int i = 0; i < projectors.Length; i++)
        {
            if (projectors[i] != null)
            {
                projectors[i].fadeFactor = value;
            }
        }
    }

    private GameObject GetRoot(DecalSlot slot)
    {
        switch (slot)
        {
            case DecalSlot.Slot_0:
                return slot0Root;
            case DecalSlot.Slot_1:
                return slot1Root;
            case DecalSlot.Slot_2:
                return slot2Root;
            case DecalSlot.Slot_3:
                return slot3Root;
            case DecalSlot.Slot_4:
                return slot4Root;
            default:
                return slot0Root;
        }
    }

    private void ApplyDoorSlot(DecalSlot slot, float duration)
    {
        if (doorSwitchers == null)
        {
            return;
        }

        Lab_DoorDecalColorSwitcher.DecalSlot doorSlot = ConvertToDoorSlot(slot);

        for (int i = 0; i < doorSwitchers.Length; i++)
        {
            if (doorSwitchers[i] != null)
            {
                doorSwitchers[i].SetSlot(doorSlot, duration);
            }
        }
    }

    private void ApplyDoorSlotInstant(DecalSlot slot)
    {
        if (doorSwitchers == null)
        {
            return;
        }

        Lab_DoorDecalColorSwitcher.DecalSlot doorSlot = ConvertToDoorSlot(slot);

        for (int i = 0; i < doorSwitchers.Length; i++)
        {
            if (doorSwitchers[i] != null)
            {
                doorSwitchers[i].SetSlot(doorSlot, 0f);
            }
        }
    }

    private Lab_DoorDecalColorSwitcher.DecalSlot ConvertToDoorSlot(DecalSlot slot)
    {
        switch (slot)
        {
            case DecalSlot.Slot_0:
                return Lab_DoorDecalColorSwitcher.DecalSlot.Slot_0;
            case DecalSlot.Slot_1:
                return Lab_DoorDecalColorSwitcher.DecalSlot.Slot_1;
            case DecalSlot.Slot_2:
                return Lab_DoorDecalColorSwitcher.DecalSlot.Slot_2;
            case DecalSlot.Slot_3:
                return Lab_DoorDecalColorSwitcher.DecalSlot.Slot_3;
            case DecalSlot.Slot_4:
                return Lab_DoorDecalColorSwitcher.DecalSlot.Slot_4;
            default:
                return Lab_DoorDecalColorSwitcher.DecalSlot.Slot_0;
        }
    }

    public DecalSlot GetCurrentSlot()
    {
        return currentSlot;
    }
}
}
