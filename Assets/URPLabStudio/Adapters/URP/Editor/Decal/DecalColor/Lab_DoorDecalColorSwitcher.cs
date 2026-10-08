namespace URPLabStudio
{
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Lab_DoorDecalColorSwitcher : MonoBehaviour
{
    public enum DecalSlot
    {
        Slot_0,
        Slot_1,
        Slot_2,
        Slot_3,
        Slot_4
    }

    [Header("Door Decal Slots")]
    [SerializeField] private GameObject slot0;
    [SerializeField] private GameObject slot1;
    [SerializeField] private GameObject slot2;
    [SerializeField] private GameObject slot3;
    [SerializeField] private GameObject slot4;

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

    public void SetSlot(DecalSlot slot)
    {
        SetSlot(slot, 0.35f);
    }

    public void SetSlot(DecalSlot slot, float duration)
    {
        if (!Application.isPlaying || duration <= 0f)
        {
            ApplyInstant(slot);
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(FadeToSlot(slot, duration));
    }

    private IEnumerator FadeToSlot(DecalSlot newSlot, float duration)
    {
        GameObject previousRoot = GetRoot(currentSlot);
        GameObject nextRoot = GetRoot(newSlot);

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

        float safeDuration = Mathf.Max(0.01f, duration);
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

        ApplyRootInstant(slot0, slot == DecalSlot.Slot_0);
        ApplyRootInstant(slot1, slot == DecalSlot.Slot_1);
        ApplyRootInstant(slot2, slot == DecalSlot.Slot_2);
        ApplyRootInstant(slot3, slot == DecalSlot.Slot_3);
        ApplyRootInstant(slot4, slot == DecalSlot.Slot_4);
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
                return slot0;
            case DecalSlot.Slot_1:
                return slot1;
            case DecalSlot.Slot_2:
                return slot2;
            case DecalSlot.Slot_3:
                return slot3;
            case DecalSlot.Slot_4:
                return slot4;
            default:
                return slot0;
        }
    }

    public DecalSlot GetCurrentSlot()
    {
        return currentSlot;
    }
}
}
