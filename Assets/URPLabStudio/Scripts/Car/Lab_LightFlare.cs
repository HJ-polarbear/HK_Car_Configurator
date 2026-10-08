namespace URPLabStudio
{
using URPLabStudio.LabMotion;
using URPLabStudio.LabMotion.Rendering;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class Lab_LightFlare : MonoBehaviour
{
    private LensFlareComponentSRP[] flares;
    private Vector3[] originalPoses;
    private float[] originalIntensities;
    private bool[] originalAllowOffScreen;

    public Transform[] children;

    private int count;
    private float positionMultiply = 2.5f;
    private bool opened;
    private bool settled;

    [Tooltip("Distance compensation for the settled flare. HDRP needs a small value to avoid bloom blowout.")]
    public float sizeMultiply = 1.2f;

    [Header("SRP Lens Flare")]
    [Tooltip("Enable depth occlusion again after each flare reaches its headlight.")]
    public bool occlusionWhenSettled = true;

    [Tooltip("Allow the flare to be rendered while it flies in from or out to off-screen.")]
    public bool allowOffScreenDuringAnimation = true;

    private void Start()
    {
        if (children == null || children.Length == 0)
            children = GetComponentsInChildren<Transform>(true)
                .Where(t => t != transform && (t.name.StartsWith("LightLF") || t.name.StartsWith("LightRF")))
                .ToArray();
        count = children != null ? children.Length : 0;
        flares = new LensFlareComponentSRP[count];
        originalPoses = new Vector3[count];
        originalIntensities = new float[count];
        originalAllowOffScreen = new bool[count];

        for (int i = 0; i < count; i++)
        {
            if (children[i] == null)
            {
                Debug.LogWarning($"Lab_LightFlare: Children Element {i} is not assigned.", this);
                continue;
            }

            originalPoses[i] = children[i].position;
            flares[i] = children[i].GetComponent<LensFlareComponentSRP>();

            if (flares[i] == null)
            {
                Debug.LogWarning($"Lab_LightFlare: {children[i].name} needs a Lens Flare (SRP) component.", children[i]);
                continue;
            }

            originalIntensities[i] = flares[i].intensity;
            originalAllowOffScreen[i] = flares[i].allowOffScreen;
        }

        SetBeginState();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void ExplodeEvent(bool on)
    {
        if (on && opened)
            TurnOff();
    }

    private void LightFlareEvent(bool on)
    {
        if (on)
            TurnOn();
        else
            TurnOff();
    }

    public void SetLights(bool on) => LightFlareEvent(on);

    private void SetBeginState()
    {
        opened = false;
        settled = false;

        for (int i = 0; i < count; i++)
        {
            if (children[i] != null)
                children[i].position = originalPoses[i] * positionMultiply;

            PrepareFlareForAnimation(i);
            SetFlareIntensity(i, 0f);
        }
    }

    private void TurnOn()
    {
        KillTweens();
        opened = true;
        settled = false;

        const float duration = 0.7f;
        const float flareDuration = 0.2f;
        int completedMoves = 0;

        for (int i = 0; i < count; i++)
        {
            int index = i;
            float delay = 0.05f * index;
            Vector3 targetPos = originalPoses[index];

            PrepareFlareForAnimation(index);

            if (children[index] != null)
            {
                Lab_TransformMotions.WorldPositionX(children[index], targetPos.x, duration, new MotionOptions(delay, Lab_Ease.InCubic));
                Lab_TransformMotions.WorldPositionZ(children[index], targetPos.z, duration, new MotionOptions(delay, Lab_Ease.InCubic));
                Lab_TransformMotions.WorldPositionY(children[index], targetPos.y, duration,
                    new MotionOptions(delay, Lab_Ease.OutSine, () =>
                    {
                        RestoreSettledOcclusion(index);
                        completedMoves++;
                        if (completedMoves >= count)
                            settled = true;
                    }));
            }
            else
            {
                completedMoves++;
            }

            TweenFlareIntensity(index, originalIntensities[index], flareDuration, delay + duration - 0.1f);
        }
    }

    private void TurnOff()
    {
        if (count == 0)
        {
            opened = false;
            return;
        }

        KillTweens();
        settled = false;

        const float duration = 0.7f;
        const float flareDuration = 0.2f;

        for (int i = 0; i < count; i++)
        {
            int index = i;
            float delay = 0.05f * index + 0.2f;
            Vector3 targetPos = originalPoses[index] * positionMultiply;

            PrepareFlareForAnimation(index);

            if (children[index] != null)
            {
                Lab_TransformMotions.WorldPositionX(children[index], targetPos.x, duration, new MotionOptions(delay, Lab_Ease.OutSine));
                Lab_TransformMotions.WorldPositionZ(children[index], targetPos.z, duration, new MotionOptions(delay, Lab_Ease.OutSine));
                Lab_TransformMotions.WorldPositionY(children[index], targetPos.y, duration,
                    new MotionOptions(delay, Lab_Ease.InCubic, index == count - 1 ? () =>
                    {
                        opened = false;
                    } : null));
            }

            int intensityIndex = index;
            TweenFlareIntensity(index, originalIntensities[index], flareDuration, delay - 0.2f, () =>
                TweenFlareIntensity(intensityIndex, 0f, flareDuration, duration));
        }
    }

    private void Update()
    {
        Camera activeCamera = Camera.main;
        if (!opened || !settled || activeCamera == null)
            return;

        float cameraDistance = Vector3.Distance(activeCamera.transform.position, transform.position);
        if (cameraDistance < 0.001f)
            return;

        float distanceMultiplier = sizeMultiply / cameraDistance;
        float lerpAmount = Time.deltaTime * 10f;

        for (int i = 0; i < count; i++)
        {
            if (flares[i] == null)
                continue;

            float targetIntensity = originalIntensities[i] * distanceMultiplier;
            flares[i].intensity = Mathf.Lerp(flares[i].intensity, targetIntensity, lerpAmount);
        }
    }

    private void PrepareFlareForAnimation(int index)
    {
        if (!HasFlare(index))
            return;

        flares[index].useOcclusion = false;
        flares[index].allowOffScreen = allowOffScreenDuringAnimation;
    }

    private void RestoreSettledOcclusion(int index)
    {
        if (!HasFlare(index))
            return;

        flares[index].useOcclusion = occlusionWhenSettled;
        flares[index].allowOffScreen = originalAllowOffScreen[index];
    }

    private Lab_MotionHandle TweenFlareIntensity(int index, float targetIntensity, float duration, float delay, System.Action onComplete = null)
    {
        if (!HasFlare(index))
            return default;

        return Lab_LensFlareMotionAdapter.Intensity(flares[index], targetIntensity, duration, delay, Lab_Ease.Linear, onComplete);
    }

    private void SetFlareIntensity(int index, float intensity)
    {
        if (HasFlare(index))
            flares[index].intensity = intensity;
    }

    private bool HasFlare(int index)
    {
        return index >= 0 && index < count && flares[index] != null;
    }

    private void KillTweens()
    {
        if (!Lab_MotionScheduler.TryGetCurrent(out Lab_MotionScheduler scheduler))
            return;

        for (int i = 0; i < count; i++)
        {
            if (children[i] != null)
            {
                scheduler.Cancel(children[i], MotionChannel.WorldPositionX);
                scheduler.Cancel(children[i], MotionChannel.WorldPositionY);
                scheduler.Cancel(children[i], MotionChannel.WorldPositionZ);
            }

            if (flares[i] != null)
                scheduler.Cancel(flares[i], MotionChannel.Float, Lab_LensFlareMotionAdapter.IntensityChannel);
        }
    }
}
}
