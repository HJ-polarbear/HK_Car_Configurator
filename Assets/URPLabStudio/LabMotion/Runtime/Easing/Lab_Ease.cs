using UnityEngine;

namespace URPLabStudio.LabMotion
{
    public enum Lab_Ease
    {
        Linear,
        InCubic,
        OutCubic,
        InOutCubic,
        OutSine
    }

    public static class LabEaseMath
    {
        public static float Evaluate(Lab_Ease ease, float time)
        {
            float t = Mathf.Clamp01(time);
            switch (ease)
            {
                case Lab_Ease.InCubic:
                    return t * t * t;
                case Lab_Ease.OutCubic:
                    {
                        float inverse = 1f - t;
                        return 1f - inverse * inverse * inverse;
                    }
                case Lab_Ease.InOutCubic:
                    if (t < 0.5f)
                        return 4f * t * t * t;
                    {
                        float inverse = -2f * t + 2f;
                        return 1f - inverse * inverse * inverse * 0.5f;
                    }
                case Lab_Ease.OutSine:
                    return Mathf.Sin(t * Mathf.PI * 0.5f);
                default:
                    return t;
            }
        }
    }
}
