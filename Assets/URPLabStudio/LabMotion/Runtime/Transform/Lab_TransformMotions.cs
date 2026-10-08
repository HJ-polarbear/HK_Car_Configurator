using UnityEngine;

namespace URPLabStudio.LabMotion
{
    public static class Lab_TransformMotions
    {
        public static Lab_MotionHandle WorldPosition(Transform target, Vector3 destination, float duration, MotionOptions options = default)
            => Lab_MotionScheduler.Current.StartTransform(target, MotionChannel.WorldPosition, destination, 0f, duration, options);

        public static Lab_MotionHandle LocalPosition(Transform target, Vector3 destination, float duration, MotionOptions options = default)
            => Lab_MotionScheduler.Current.StartTransform(target, MotionChannel.LocalPosition, destination, 0f, duration, options);

        public static Lab_MotionHandle Scale(Transform target, Vector3 destination, float duration, MotionOptions options = default)
            => Lab_MotionScheduler.Current.StartTransform(target, MotionChannel.Scale, destination, 0f, duration, options);

        public static Lab_MotionHandle WorldPositionX(Transform target, float destination, float duration, MotionOptions options = default)
            => Lab_MotionScheduler.Current.StartTransform(target, MotionChannel.WorldPositionX, default, destination, duration, options);

        public static Lab_MotionHandle WorldPositionY(Transform target, float destination, float duration, MotionOptions options = default)
            => Lab_MotionScheduler.Current.StartTransform(target, MotionChannel.WorldPositionY, default, destination, duration, options);

        public static Lab_MotionHandle WorldPositionZ(Transform target, float destination, float duration, MotionOptions options = default)
            => Lab_MotionScheduler.Current.StartTransform(target, MotionChannel.WorldPositionZ, default, destination, duration, options);
    }
}
