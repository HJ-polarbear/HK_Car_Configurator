using System;

namespace URPLabStudio.LabMotion
{
    public enum MotionChannel
    {
        WorldPosition,
        WorldPositionX,
        WorldPositionY,
        WorldPositionZ,
        LocalPosition,
        Scale,
        Float
    }

    public enum LabMotionState
    {
        Invalid,
        Running,
        Completed,
        Cancelled
    }

    public readonly struct MotionOptions
    {
        public readonly float Delay;
        public readonly Lab_Ease Ease;
        public readonly Action OnComplete;
        public readonly int CustomChannel;

        public MotionOptions(
            float delay = 0f,
            Lab_Ease ease = Lab_Ease.Linear,
            Action onComplete = null,
            int customChannel = 0)
        {
            Delay = delay < 0f ? 0f : delay;
            Ease = ease;
            OnComplete = onComplete;
            CustomChannel = customChannel;
        }
    }
}
