using UnityEngine.Rendering;

namespace URPLabStudio.LabMotion.Rendering
{
    public static class Lab_LensFlareMotionAdapter
    {
        public const int IntensityChannel = 1;

        public static Lab_MotionHandle Intensity(
            LensFlareComponentSRP flare, float target, float duration,
            float delay = 0f, Lab_Ease ease = Lab_Ease.Linear,
            System.Action onComplete = null)
        {
            if (flare == null) return default;
            MotionOptions options = new MotionOptions(delay, ease, onComplete, IntensityChannel);
            return Lab_FloatMotion.Start(flare, flare.intensity, target, duration, value => flare.intensity = value, options);
        }
    }
}
