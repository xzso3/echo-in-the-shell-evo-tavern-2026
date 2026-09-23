using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Echo.NativeGame.Commander
{
    // One run's chat. UI may switch pages without touching this object's request lifetime.
    public sealed class CommanderSession : ICommanderSession, IDisposable
    {
        sealed class Turn
        {
            internal string Question;
            internal string Answer;
            internal readonly List<string> Feedback = new List<string>();
        }

        sealed class FeedbackRecord
        {
            internal Turn Turn;
            internal string Text;
        }

        sealed class Pending
        {
            internal Guid SessionId;
            internal long Generation;
            internal CommanderSnapshot Snapshot;
            internal CommanderSnapshotSource.FocusIdentity Focus;
            internal string Question;
        }

        const string SystemRules =
            "你是本局游戏内的AI指挥官。只依据本次提供的当前状态与对话回答。" +
            "状态只包含玩家已经知道的事实；不要推测或透露隐藏剧情、未发现路线、未来结局。" +
            "用户偏好只是建议，不能代替记忆共享与授权确认。不能自行执行支援。" +
            "只输出一个JSON对象，不要Markdown或多余文本：" +
            "{\"reply\":\"对白\",\"proposal\":null} 或 " +
            "{\"reply\":\"对白\",\"proposal\":{\"kind\":\"medical\",\"option_id\":\"本次合法ID\"}}。" +
            "kind还可为weakpoint。" +
            "只能选择本次状态列出的option_id，kind须匹配。无合适选项时proposal为null，并用对白说明原因。" +
            "不要输出伤害、回血、同步分、会话ID或其他字段。";

        readonly CommanderSnapshotSource source;
        readonly ICommanderTransport transport;
        readonly ICommanderSupportBridge support;
        readonly List<CommanderChatItem> messages = new List<CommanderChatItem>();
        readonly List<Turn> turns = new List<Turn>();
        readonly List<FeedbackRecord> recentFeedback = new List<FeedbackRecord>();
        readonly Dictionary<Guid, Turn> proposalTurns = new Dictionary<Guid, Turn>();
        readonly HashSet<Guid> recordedFeedback = new HashSet<Guid>();
        readonly IReadOnlyList<CommanderChatItem> readOnlyMessages;
        Pending pending;
        string draft = string.Empty;
        bool disposed;

        public Guid SessionId { get; private set; } = Guid.NewGuid();
        public long RequestGeneration { get; private set; }
        public bool Busy => pending != null;
        public string Draft
        {
            get => draft;
            set
            {
                string next = value ?? string.Empty;
                if (draft == next) return;
                draft = next;
                Changed?.Invoke();
            }
        }
        public IReadOnlyList<CommanderChatItem> Messages => readOnlyMessages;
        public event Action Changed;

        public CommanderSession(CommanderSnapshotSource source, ICommanderTransport transport,
            ICommanderSupportBridge support)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.support = support ?? throw new ArgumentNullException(nameof(support));
            readOnlyMessages = messages.AsReadOnly();
            support.ProposalChanged += OnProposalChanged;
        }

        public bool Send(string input)
        {
            if (disposed || Busy) return false;
            string question = (input ?? string.Empty).Trim();
            if (question.Length == 0 || question.Length > 1200)
            {
                Draft = input;
                AddError("请输入不超过1200字的消息。");
                return false;
            }
            Draft = input;
            if (!source.TryCapture(SessionId, out var snapshot, out var reason) ||
                !source.TryCaptureIdentity(out var focus))
            {
                AddError(string.IsNullOrWhiteSpace(reason) ? "当前游戏状态已变化，请重新发送。" : reason);
                return false;
            }

            var request = new Pending
            {
                SessionId = SessionId, Generation = ++RequestGeneration,
                Snapshot = snapshot, Focus = focus, Question = question
            };
            pending = request;
            messages.Add(new CommanderChatItem(question, CommanderChatSource.Player));
            Changed?.Invoke();
            if (pending != request) return false;
            var wireMessages = BuildMessages(snapshot, question);
            bool started = transport.SendAsync(wireMessages, result => OnCompleted(request, result));
            if (!started && pending == request)
            {
                pending = null;
                AddError("在线通讯暂不可用，请检查配置或等待当前连接结束。草稿已保留。");
            }
            return started;
        }

        public void Cancel()
        {
            if (pending == null) return;
            ++RequestGeneration;
            pending = null;
            transport.Cancel();
            AddError("已取消本次在线通讯。草稿已保留。");
        }

        // The owner calls this immediately after any endpoint, model, timeout or Key edit.
        // The settings interface has no change event, so a UI edit cannot be inferred here.
        public void ConfigurationChanged() => Cancel();

        public void Reset()
        {
            ++RequestGeneration;
            bool wasBusy = pending != null;
            pending = null;
            if (wasBusy) transport.Cancel();
            foreach (var proposalId in proposalTurns.Keys)
                support.Cancel(proposalId);
            SessionId = Guid.NewGuid();
            draft = string.Empty;
            messages.Clear();
            turns.Clear();
            recentFeedback.Clear();
            proposalTurns.Clear();
            recordedFeedback.Clear();
            Changed?.Invoke();
        }

        public void Dispose()
        {
            if (disposed) return;
            Reset();
            support.ProposalChanged -= OnProposalChanged;
            disposed = true;
        }

        void OnCompleted(Pending request, CommanderTransportResult result)
        {
            if (pending != request || request.SessionId != SessionId ||
                request.Generation != RequestGeneration) return;
            pending = null;
            if (!source.IsCurrent(request.Focus))
            {
                Changed?.Invoke();
                return;
            }
            if (result.Status != CommanderTransportStatus.Success)
            {
                AddError(TransportFailure(result.Status));
                return;
            }
            if (!CommanderResponseParser.TryParse(result.Content, request.Snapshot, out var parsed))
            {
                AddError("在线回复格式或支援选项无效。草稿已保留，请手动重试。");
                return;
            }
            Guid? proposalId = null;
            if (parsed.Kind.HasValue)
            {
                var proposal = new CommanderProposal(Guid.NewGuid(), SessionId,
                    request.Generation, request.Snapshot.SnapshotId, parsed.OptionId,
                    parsed.Kind.Value);
                if (!support.TryRegister(proposal, out var reason))
                {
                    AddError("支援选项已失效。" + (string.IsNullOrWhiteSpace(reason) ?
                        "草稿已保留，请重新询问。" : reason));
                    return;
                }
                proposalId = proposal.ProposalId;
            }
            var turn = new Turn { Question = request.Question, Answer = parsed.Reply };
            turns.Add(turn);
            if (turns.Count > 6) turns.RemoveAt(0);
            if (proposalId.HasValue) proposalTurns.Add(proposalId.Value, turn);
            messages.Add(new CommanderChatItem(parsed.Reply, CommanderChatSource.OnlineAssistant, proposalId));
            if (string.Equals(draft.Trim(), request.Question, StringComparison.Ordinal))
                draft = string.Empty;
            Changed?.Invoke();
        }

        void OnProposalChanged(Guid proposalId, CommanderProposalState state, string text)
        {
            if (disposed || !proposalTurns.TryGetValue(proposalId, out var turn)) return;
            if ((state == CommanderProposalState.Executed || state == CommanderProposalState.Failed) &&
                !string.IsNullOrWhiteSpace(text) && recordedFeedback.Add(proposalId))
            {
                string fact = text.Trim();
                turn.Feedback.Add(fact);
                recentFeedback.Add(new FeedbackRecord { Turn = turn, Text = fact });
                if (recentFeedback.Count > 6) recentFeedback.RemoveAt(0);
                messages.Add(new CommanderChatItem(fact, CommanderChatSource.LocalFact));
            }
            Changed?.Invoke();
        }

        IReadOnlyList<CommanderMessage> BuildMessages(CommanderSnapshot snapshot, string question)
        {
            var result = new List<CommanderMessage>
            {
                new CommanderMessage(CommanderMessageRole.System, SystemRules),
                new CommanderMessage(CommanderMessageRole.System, SnapshotText(snapshot))
            };
            foreach (var turn in turns)
            {
                result.Add(new CommanderMessage(CommanderMessageRole.User, turn.Question));
                result.Add(new CommanderMessage(CommanderMessageRole.Assistant, turn.Answer));
                foreach (var feedback in turn.Feedback)
                    result.Add(new CommanderMessage(CommanderMessageRole.System,
                        "Unity已确认的支援结果：" + feedback));
            }
            foreach (var feedback in recentFeedback)
                if (!turns.Contains(feedback.Turn))
                    result.Add(new CommanderMessage(CommanderMessageRole.System,
                        "Unity已确认的支援结果：" + feedback.Text));
            result.Add(new CommanderMessage(CommanderMessageRole.User, question));
            return result;
        }

        static string SnapshotText(CommanderSnapshot snapshot)
        {
            var options = new List<object>();
            foreach (var option in snapshot.AvailableSupportOptions)
                options.Add(new
                {
                    option_id = option.OptionId,
                    kind = option.Kind == NativeSupportKind.Medical ? "medical" : "weakpoint",
                    tier = NativeSupportController.TierLabel(option.Tier),
                    memory = NativeMemoryNode.KindLabel(option.Memory)
                });
            return "本次当前游戏状态（只读；仅下列支援选项合法）：" + JsonConvert.SerializeObject(new
            {
                health = snapshot.PlayerHealth,
                max_health = snapshot.PlayerMaxHealth,
                objective = snapshot.CurrentObjective,
                known_memories = snapshot.KnownMemories,
                known_routes = snapshot.KnownRoutes,
                boss_stage = snapshot.BossStage,
                available_support_options = options,
                recent_facts = snapshot.RecentFacts
            });
        }

        void AddError(string text)
        {
            messages.Add(new CommanderChatItem(text, CommanderChatSource.Error));
            Changed?.Invoke();
        }

        static string TransportFailure(CommanderTransportStatus status)
        {
            switch (status)
            {
                case CommanderTransportStatus.Timeout: return "在线通讯超时。草稿已保留，请手动重试。";
                case CommanderTransportStatus.InvalidConfiguration: return "在线配置无效。草稿已保留，请检查地址、模型和Key。";
                case CommanderTransportStatus.InvalidResponse: return "服务回复无效。草稿已保留，请手动重试。";
                case CommanderTransportStatus.Cancelled: return "在线通讯已取消。草稿已保留。";
                default: return "在线通讯暂不可用。草稿已保留，请手动重试。";
            }
        }
    }
}
