using System;
namespace Echo.NativeGame
{
    public enum NativeSupportKind { Medical, Weakpoint }
    public enum NativeSupportTier { Limited, Deep }
    // Issued only after Support revalidates and applies a real effect. Each kind succeeds at most once per run.
    public readonly struct NativeSupportAuthorization
    {
        public readonly NativeSupportKind Kind;
        public readonly NativeSupportTier Tier;
        public readonly NativeMemoryKind SharedMemory;
        public readonly int SyncDelta;
        public NativeSupportAuthorization(NativeSupportKind kind, NativeSupportTier tier, NativeMemoryKind sharedMemory, int syncDelta)
        { Kind = kind; Tier = tier; SharedMemory = sharedMemory; SyncDelta = syncDelta; }
    }
    public interface INativeSupport
    {
        event Action<NativeSupportAuthorization> Authorized;
        bool HasPending { get; }
        // Changes only when a new contract is opened. Consumers can distinguish their
        // contract from a later manual one without taking ownership of the support UI.
        long PendingVersion { get; }
        string StatusText { get; }
        // Read-only intrinsic eligibility; HasPending is checked separately by callers.
        bool CanApply(NativeSupportKind kind, NativeSupportTier tier,
            NativeMemoryKind sharedMemory, out string reason);
        bool Request(NativeSupportKind kind, NativeSupportTier tier, NativeMemoryKind sharedMemory);
        bool Confirm();
        void Cancel();
    }
}
