using System;
using System.Collections.Generic;

namespace Echo.NativeGame.Commander
{
    public enum CommanderChatSource { Player, OnlineAssistant, LocalFact, Error, LocalTopic }

    public sealed class CommanderChatItem
    {
        public string Text { get; }
        public CommanderChatSource Source { get; }
        public Guid? ProposalId { get; }

        public CommanderChatItem(string text, CommanderChatSource source, Guid? proposalId = null)
        {
            Text = text ?? string.Empty;
            Source = source;
            ProposalId = proposalId;
        }
    }

    public interface ICommanderSession
    {
        Guid SessionId { get; }
        long RequestGeneration { get; }
        bool Busy { get; }
        string Draft { get; set; }
        IReadOnlyList<CommanderChatItem> Messages { get; }
        event Action Changed;
        bool Send(string input);
        void ShowLocalTopic(string text);
        void Cancel();
        void Reset();
    }
}
