using System;
using System.Collections.Generic;

namespace Echo.NativeGame.Commander
{
    // Shared values only. Transport, session, support and UI own their own lifetimes.
    public enum CommanderMessageRole { System, User, Assistant }

    public readonly struct CommanderMessage
    {
        public CommanderMessageRole Role { get; }
        public string Content { get; }

        public CommanderMessage(CommanderMessageRole role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    public enum CommanderTransportStatus
    {
        Success, Cancelled, InvalidConfiguration, Timeout, NetworkError, HttpError, InvalidResponse
    }

    public readonly struct CommanderTransportResult
    {
        // Content is choices[0].message.content, not the parsed game reply.
        public CommanderTransportStatus Status { get; }
        public string Content { get; }
        public string ErrorText { get; }
        public int HttpStatus { get; }

        public CommanderTransportResult(CommanderTransportStatus status, string content,
            string errorText = null, int httpStatus = 0)
        {
            Status = status;
            Content = content;
            ErrorText = errorText;
            HttpStatus = httpStatus;
        }
    }

    public readonly struct CommanderSupportOption
    {
        // One opaque ID for one captured snapshot. Never a model-chosen game value.
        public Guid SnapshotId { get; }
        public string OptionId { get; }
        public NativeSupportKind Kind { get; }
        public NativeSupportTier Tier { get; }
        public NativeMemoryKind Memory { get; }

        public CommanderSupportOption(Guid snapshotId, string optionId,
            NativeSupportKind kind, NativeSupportTier tier, NativeMemoryKind memory)
        {
            SnapshotId = snapshotId;
            OptionId = optionId;
            Kind = kind;
            Tier = tier;
            Memory = memory;
        }
    }

    public sealed class CommanderSnapshot
    {
        public Guid SessionId { get; }
        public Guid SnapshotId { get; }
        public float PlayerHealth { get; }
        public float PlayerMaxHealth { get; }
        public string CurrentObjective { get; }
        public IReadOnlyList<string> KnownMemories { get; }
        public IReadOnlyList<string> KnownRoutes { get; }
        public string BossStage { get; }
        public IReadOnlyList<CommanderSupportOption> AvailableSupportOptions { get; }
        public IReadOnlyList<string> RecentFacts { get; }

        public CommanderSnapshot(Guid sessionId, Guid snapshotId, float playerHealth,
            float playerMaxHealth, string currentObjective, IReadOnlyList<string> knownMemories,
            IReadOnlyList<string> knownRoutes, string bossStage,
            IReadOnlyList<CommanderSupportOption> availableSupportOptions,
            IReadOnlyList<string> recentFacts)
        {
            SessionId = sessionId;
            SnapshotId = snapshotId;
            PlayerHealth = playerHealth;
            PlayerMaxHealth = playerMaxHealth;
            CurrentObjective = currentObjective;
            KnownMemories = knownMemories;
            KnownRoutes = knownRoutes;
            BossStage = bossStage;
            AvailableSupportOptions = availableSupportOptions;
            RecentFacts = recentFacts;
        }
    }

    public enum CommanderProposalState
    {
        Available, Viewing, Executed, Rejected, Cancelled, Expired, Failed
    }

    public readonly struct CommanderProposal
    {
        public Guid ProposalId { get; }
        public Guid SessionId { get; }
        public long RequestGeneration { get; }
        public Guid SnapshotId { get; }
        public string OptionId { get; }
        public NativeSupportKind Kind { get; }

        public CommanderProposal(Guid proposalId, Guid sessionId, long requestGeneration,
            Guid snapshotId, string optionId, NativeSupportKind kind)
        {
            ProposalId = proposalId;
            SessionId = sessionId;
            RequestGeneration = requestGeneration;
            SnapshotId = snapshotId;
            OptionId = optionId;
            Kind = kind;
        }
    }
}
