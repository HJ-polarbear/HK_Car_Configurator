using System;
using UnityEngine;

namespace URPLabStudio.LabMotion
{
    public static class Lab_FloatMotion
    {
        public static Lab_MotionHandle Start(
            UnityEngine.Object owner, float startValue, float targetValue, float duration,
            Action<float> setter, MotionOptions options = default)
        {
            return Lab_MotionScheduler.Current.StartFloat(
                owner, options.CustomChannel, startValue, targetValue, duration, setter, options);
        }
    }
}
