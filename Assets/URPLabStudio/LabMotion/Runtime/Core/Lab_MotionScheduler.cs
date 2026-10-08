using System;
using System.Collections.Generic;
using UnityEngine;

namespace URPLabStudio.LabMotion
{
    [AddComponentMenu("")]
    public sealed class Lab_MotionScheduler : MonoBehaviour
    {
        private enum MotionKind { WorldPosition, WorldAxis, LocalPosition, Scale, Float }

        private sealed class Slot
        {
            public uint Generation = 1;
            public LabMotionState State = LabMotionState.Invalid;
            public bool Active;
            public UnityEngine.Object Owner;
            public Transform Transform;
            public MotionKind Kind;
            public MotionChannel Channel;
            public int CustomChannel;
            public Vector3 StartVector;
            public Vector3 TargetVector;
            public float StartFloat;
            public float TargetFloat;
            public float Duration;
            public float Delay;
            public float Elapsed;
            public Lab_Ease Ease;
            public Action<float> FloatSetter;
            public Action Completion;

            public void ClearPayload()
            {
                Owner = null;
                Transform = null;
                FloatSetter = null;
                Completion = null;
            }
        }

        private sealed class StaticState { public Lab_MotionScheduler Current; }
        private static readonly StaticState State = new StaticState();
        private readonly List<Slot> slots = new List<Slot>(64);
        private readonly List<int> activeSlots = new List<int>(64);
        private int completedCount;
        private int cancelledCount;

        public static Lab_MotionScheduler Current
        {
            get
            {
                if (State.Current != null) return State.Current;
                GameObject host = new GameObject("LabMotion Scheduler");
                State.Current = host.AddComponent<Lab_MotionScheduler>();
                return State.Current;
            }
        }

        public static bool TryGetCurrent(out Lab_MotionScheduler scheduler)
        {
            scheduler = State.Current;
            return scheduler != null;
        }

        public int ActiveCount => activeSlots.Count;
        public int CompletedCount => completedCount;
        public int CancelledCount => cancelledCount;

        private void Awake()
        {
            if (State.Current != null && State.Current != this)
            {
                Destroy(gameObject);
                return;
            }
            State.Current = this;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        internal void Tick(float deltaTime)
        {
            float safeDelta = deltaTime < 0f ? 0f : deltaTime;
            int index = 0;
            while (index < activeSlots.Count)
            {
                int slotIndex = activeSlots[index];
                Slot slot = slots[slotIndex];
                if (!slot.Active)
                {
                    RemoveActiveAt(index);
                    continue;
                }
                if (slot.Owner == null)
                {
                    FinishCancelled(slot);
                    RemoveActiveAt(index);
                    continue;
                }

                slot.Elapsed += safeDelta;
                if (slot.Elapsed < slot.Delay)
                {
                    index++;
                    continue;
                }

                float motionTime = slot.Elapsed - slot.Delay;
                float normalized = slot.Duration <= 0f ? 1f : Mathf.Clamp01(motionTime / slot.Duration);
                Apply(slot, LabEaseMath.Evaluate(slot.Ease, normalized), normalized >= 1f);
                if (normalized < 1f)
                {
                    index++;
                    continue;
                }

                Action callback = slot.Completion;
                slot.State = LabMotionState.Completed;
                slot.Active = false;
                slot.ClearPayload();
                completedCount++;
                RemoveActiveAt(index);
                callback?.Invoke();
            }
        }

        private static void Apply(Slot slot, float eased, bool exactFinal)
        {
            switch (slot.Kind)
            {
                case MotionKind.WorldPosition:
                    slot.Transform.position = exactFinal ? slot.TargetVector : Vector3.LerpUnclamped(slot.StartVector, slot.TargetVector, eased);
                    break;
                case MotionKind.WorldAxis:
                    {
                        Vector3 position = slot.Transform.position;
                        float value = exactFinal ? slot.TargetFloat : Mathf.LerpUnclamped(slot.StartFloat, slot.TargetFloat, eased);
                        if (slot.Channel == MotionChannel.WorldPositionX) position.x = value;
                        else if (slot.Channel == MotionChannel.WorldPositionY) position.y = value;
                        else position.z = value;
                        slot.Transform.position = position;
                        break;
                    }
                case MotionKind.LocalPosition:
                    slot.Transform.localPosition = exactFinal ? slot.TargetVector : Vector3.LerpUnclamped(slot.StartVector, slot.TargetVector, eased);
                    break;
                case MotionKind.Scale:
                    slot.Transform.localScale = exactFinal ? slot.TargetVector : Vector3.LerpUnclamped(slot.StartVector, slot.TargetVector, eased);
                    break;
                case MotionKind.Float:
                    slot.FloatSetter(exactFinal ? slot.TargetFloat : Mathf.LerpUnclamped(slot.StartFloat, slot.TargetFloat, eased));
                    break;
            }
        }

        internal Lab_MotionHandle StartTransform(
            Transform target, MotionChannel channel, Vector3 targetVector,
            float targetFloat, float duration, MotionOptions options)
        {
            if (target == null) return default;
            CancelConflicts(target, channel, 0);
            Slot slot = Allocate();
            slot.Owner = target;
            slot.Transform = target;
            slot.Channel = channel;
            slot.Kind = channel == MotionChannel.WorldPosition ? MotionKind.WorldPosition
                : channel == MotionChannel.LocalPosition ? MotionKind.LocalPosition
                : channel == MotionChannel.Scale ? MotionKind.Scale : MotionKind.WorldAxis;
            slot.StartVector = channel == MotionChannel.LocalPosition ? target.localPosition
                : channel == MotionChannel.Scale ? target.localScale : target.position;
            slot.TargetVector = targetVector;
            slot.StartFloat = channel == MotionChannel.WorldPositionX ? target.position.x
                : channel == MotionChannel.WorldPositionY ? target.position.y : target.position.z;
            slot.TargetFloat = targetFloat;
            Configure(slot, duration, options);
            return Activate(slot);
        }

        internal Lab_MotionHandle StartFloat(
            UnityEngine.Object owner, int customChannel, float start, float target,
            float duration, Action<float> setter, MotionOptions options)
        {
            if (owner == null || setter == null) return default;
            CancelConflicts(owner, MotionChannel.Float, customChannel);
            Slot slot = Allocate();
            slot.Owner = owner;
            slot.Channel = MotionChannel.Float;
            slot.CustomChannel = customChannel;
            slot.Kind = MotionKind.Float;
            slot.StartFloat = start;
            slot.TargetFloat = target;
            slot.FloatSetter = setter;
            Configure(slot, duration, options);
            return Activate(slot);
        }

        private static void Configure(Slot slot, float duration, MotionOptions options)
        {
            slot.Duration = duration < 0f ? 0f : duration;
            slot.Delay = options.Delay;
            slot.Elapsed = 0f;
            slot.Ease = options.Ease;
            slot.Completion = options.OnComplete;
        }

        private Slot Allocate()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                Slot candidate = slots[i];
                if (candidate.Active || candidate.State == LabMotionState.Running) continue;
                candidate.Generation++;
                if (candidate.Generation == 0) candidate.Generation = 1;
                candidate.State = LabMotionState.Invalid;
                return candidate;
            }
            Slot created = new Slot();
            slots.Add(created);
            return created;
        }

        private Lab_MotionHandle Activate(Slot slot)
        {
            int index = slots.IndexOf(slot);
            slot.State = LabMotionState.Running;
            slot.Active = true;
            activeSlots.Add(index);
            return new Lab_MotionHandle(this, index, slot.Generation);
        }

        private void CancelConflicts(UnityEngine.Object owner, MotionChannel channel, int customChannel)
        {
            for (int i = activeSlots.Count - 1; i >= 0; i--)
            {
                Slot slot = slots[activeSlots[i]];
                if (slot.Owner != owner || !ChannelsConflict(slot, channel, customChannel)) continue;
                FinishCancelled(slot);
                RemoveActiveAt(i);
            }
        }

        private static bool ChannelsConflict(Slot existing, MotionChannel incoming, int customChannel)
        {
            if (incoming == MotionChannel.Float)
                return existing.Channel == MotionChannel.Float && existing.CustomChannel == customChannel;
            if (incoming == MotionChannel.WorldPosition)
                return existing.Channel == MotionChannel.WorldPosition || IsWorldAxis(existing.Channel);
            if (IsWorldAxis(incoming))
                return existing.Channel == MotionChannel.WorldPosition || existing.Channel == incoming;
            return existing.Channel == incoming;
        }

        private static bool IsWorldAxis(MotionChannel channel)
        {
            return channel == MotionChannel.WorldPositionX || channel == MotionChannel.WorldPositionY || channel == MotionChannel.WorldPositionZ;
        }

        public int CancelTarget(UnityEngine.Object owner)
        {
            if (owner == null) return 0;
            int count = 0;
            for (int i = activeSlots.Count - 1; i >= 0; i--)
            {
                Slot slot = slots[activeSlots[i]];
                if (slot.Owner != owner) continue;
                FinishCancelled(slot);
                RemoveActiveAt(i);
                count++;
            }
            return count;
        }

        public bool Cancel(UnityEngine.Object owner, MotionChannel channel, int customChannel = 0)
        {
            if (owner == null) return false;
            bool cancelled = false;
            for (int i = activeSlots.Count - 1; i >= 0; i--)
            {
                Slot slot = slots[activeSlots[i]];
                if (slot.Owner != owner || slot.Channel != channel) continue;
                if (channel == MotionChannel.Float && slot.CustomChannel != customChannel) continue;
                FinishCancelled(slot);
                RemoveActiveAt(i);
                cancelled = true;
            }
            return cancelled;
        }

        internal bool Cancel(int slotIndex, uint generation)
        {
            if (!TryGet(slotIndex, generation, out Slot slot) || !slot.Active) return false;
            FinishCancelled(slot);
            for (int i = activeSlots.Count - 1; i >= 0; i--)
                if (activeSlots[i] == slotIndex) { RemoveActiveAt(i); break; }
            return true;
        }

        internal LabMotionState GetState(int slotIndex, uint generation)
        {
            return TryGet(slotIndex, generation, out Slot slot) ? slot.State : LabMotionState.Invalid;
        }

        private bool TryGet(int slotIndex, uint generation, out Slot slot)
        {
            if (slotIndex >= 0 && slotIndex < slots.Count)
            {
                slot = slots[slotIndex];
                return slot.Generation == generation;
            }
            slot = null;
            return false;
        }

        private void FinishCancelled(Slot slot)
        {
            if (!slot.Active) return;
            slot.State = LabMotionState.Cancelled;
            slot.Active = false;
            slot.ClearPayload();
            cancelledCount++;
        }

        private void RemoveActiveAt(int index)
        {
            int last = activeSlots.Count - 1;
            activeSlots[index] = activeSlots[last];
            activeSlots.RemoveAt(last);
        }

        private void OnDestroy()
        {
            for (int i = activeSlots.Count - 1; i >= 0; i--)
                FinishCancelled(slots[activeSlots[i]]);
            activeSlots.Clear();
            if (State.Current == this) State.Current = null;
        }
    }
}
