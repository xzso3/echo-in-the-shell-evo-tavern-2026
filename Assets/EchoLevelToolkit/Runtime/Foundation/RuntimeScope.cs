using System;

namespace Echo.LevelToolkit.Foundation
{
    // These IDs exist only for one play session. They are never serialized into content assets.
    public readonly struct RunId : IEquatable<RunId>
    {
        private readonly string value;
        private RunId(string value) { this.value = value; }
        public bool IsValid => !string.IsNullOrEmpty(value);
        public static RunId New() => new RunId(Guid.NewGuid().ToString("N"));
        public bool Equals(RunId other) => string.Equals(value, other.value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RunId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
        public override string ToString() => value ?? string.Empty;
        public static bool operator ==(RunId left, RunId right) => left.Equals(right);
        public static bool operator !=(RunId left, RunId right) => !left.Equals(right);
    }

    public readonly struct LevelInstanceId : IEquatable<LevelInstanceId>
    {
        private readonly string value;
        private LevelInstanceId(string value) { this.value = value; }
        public bool IsValid => !string.IsNullOrEmpty(value);
        public static LevelInstanceId New() => new LevelInstanceId(Guid.NewGuid().ToString("N"));
        public bool Equals(LevelInstanceId other) => string.Equals(value, other.value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LevelInstanceId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
        public override string ToString() => value ?? string.Empty;
        public static bool operator ==(LevelInstanceId left, LevelInstanceId right) => left.Equals(right);
        public static bool operator !=(LevelInstanceId left, LevelInstanceId right) => !left.Equals(right);
    }

    public readonly struct RuntimeScope : IEquatable<RuntimeScope>
    {
        public RunId RunId { get; }
        public LevelInstanceId InstanceId { get; }
        public ContentIdentity Content { get; }

        private RuntimeScope(RunId runId, LevelInstanceId instanceId, ContentIdentity content)
        {
            RunId = runId;
            InstanceId = instanceId;
            Content = content;
        }

        public static RuntimeScope New(RunId runId, ContentIdentity content)
        {
            if (!runId.IsValid) throw new ArgumentException("A valid run ID is required.", nameof(runId));
            if (!content.IsComplete) throw new ArgumentException("A complete content identity is required.", nameof(content));
            return new RuntimeScope(runId, LevelInstanceId.New(), content);
        }

        // Runtime routing compares both IDs. Content is descriptive and cannot address an instance.
        public bool Equals(RuntimeScope other) => RunId == other.RunId && InstanceId == other.InstanceId;
        public override bool Equals(object obj) => obj is RuntimeScope other && Equals(other);
        public override int GetHashCode()
        {
            unchecked { return RunId.GetHashCode() * 397 ^ InstanceId.GetHashCode(); }
        }
        public static bool operator ==(RuntimeScope left, RuntimeScope right) => left.Equals(right);
        public static bool operator !=(RuntimeScope left, RuntimeScope right) => !left.Equals(right);
    }
}
