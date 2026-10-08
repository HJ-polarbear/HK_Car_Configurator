namespace URPLabStudio.LabMotion
{
    public readonly struct Lab_MotionHandle
    {
        private readonly Lab_MotionScheduler scheduler;
        private readonly int slot;
        private readonly uint generation;

        internal Lab_MotionHandle(Lab_MotionScheduler scheduler, int slot, uint generation)
        {
            this.scheduler = scheduler;
            this.slot = slot;
            this.generation = generation;
        }

        public bool IsValid => scheduler != null && scheduler.GetState(slot, generation) != LabMotionState.Invalid;
        public bool IsRunning => scheduler != null && scheduler.GetState(slot, generation) == LabMotionState.Running;
        public bool IsCompleted => scheduler != null && scheduler.GetState(slot, generation) == LabMotionState.Completed;
        public bool IsCancelled => scheduler != null && scheduler.GetState(slot, generation) == LabMotionState.Cancelled;
        public LabMotionState State => scheduler == null ? LabMotionState.Invalid : scheduler.GetState(slot, generation);

        public bool Cancel()
        {
            return scheduler != null && scheduler.Cancel(slot, generation);
        }
    }
}
