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
            internal string WireAnswer;
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
            internal CommanderDiagnostics Trace;
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
            "不要输出伤害、回血、同步分、会话ID或其他字段。" +
            "不要输出思考过程或think标签。历史提案只供对话理解，旧option_id不可复用；当前支援只能使用本次状态列出的ID。";

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
        public CommanderDiagnostics DiagnosticTrace { get; private set; }
        string draft = string.Empty;
        bool disposed;
        public string LastOnlineStatus { get; private set; } = "未验证";

        public Guid SessionId { get; private set; } = Guid.NewGuid();
        public long RequestGeneration { get; private set; }
        public bool Busy => pending != null;
        public bool TransportBusy => transport.Busy;
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
            var trace = new CommanderDiagnostics(SessionId, RequestGeneration + 1);
            DiagnosticTrace = trace;
            trace.Log("session.send", "disposed=" + disposed + " busy=" + Busy + " input=" + input);
            if (disposed || Busy) { trace.Log("send.rejected", "disposed_or_busy"); return false; }
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

            trace.Log("snapshot", "snapshot=" + snapshot.SnapshotId + " options=" + snapshot.AvailableSupportOptions.Count);
            bool onlineConfigured = !CommanderLaunchState.OfflineForCurrentRun &&
                CommanderSettings.Instance.HasKey &&
                !string.IsNullOrEmpty(CommanderSettings.Instance.EndpointUrl) &&
                !string.IsNullOrWhiteSpace(CommanderSettings.Instance.ModelId);
            trace.Log("configuration", "onlineConfigured=" + onlineConfigured + " offline=" + CommanderLaunchState.OfflineForCurrentRun +
                " hasKey=" + CommanderSettings.Instance.HasKey + " endpoint=" + CommanderDiagnostics.Endpoint(CommanderSettings.Instance.EndpointUrl) +
                " model=" + CommanderSettings.Instance.ModelId + " timeoutSeconds=" + CommanderSettings.Instance.TimeoutSeconds);
            if (onlineConfigured && transport.Busy)
            {
                LastOnlineStatus = "待重试";
                AddError("在线通道正在通讯，请等待设置页连接测试或当前请求结束后手动重试。草稿已保留。");
                return false;
            }

            var request = new Pending
            {
                SessionId = SessionId, Generation = ++RequestGeneration,
                Snapshot = snapshot, Focus = focus, Question = question, Trace = trace
            };
            pending = request;
            messages.Add(new CommanderChatItem(question, CommanderChatSource.Player));
            Changed?.Invoke();
            if (pending != request) return false;
            if (!onlineConfigured)
            {
                CompleteFallback(request, "在线通讯未配置或本局选择离线试玩。");
                return true;
            }
            var wireMessages = BuildMessages(snapshot, question);
            CommanderDiagnostics.Bind(wireMessages, trace);
            bool started = transport.SendAsync(wireMessages, result => OnCompleted(request, result));
            if (!started && pending == request)
            {
                if (transport.Busy) CompleteBusy(request);
                else CompleteFallback(request, "在线通讯未能启动；请检查连接或稍后手动重试。");
            }
            return true;
        }

        void CompleteBusy(Pending request)
        {
            if (pending != request || request.SessionId != SessionId ||
                request.Generation != RequestGeneration) { request.Trace.Log("discard", "pending/session/generation mismatch; no fallback"); return; }
            DiagnosticTrace = request.Trace;
            pending = null;
            if (!source.IsCurrent(request.Focus))
            {
                request.Trace.Log("discard", "focus_changed; no fallback");
                DropStaleReply();
                return;
            }
            LastOnlineStatus = "待重试";
            AddError("在线通道正在通讯，请等待设置页连接测试或当前请求结束后手动重试。草稿已保留。");
        }

        public void ShowLocalTopic(string text)
        {
            if (disposed || string.IsNullOrWhiteSpace(text)) return;
            messages.Add(new CommanderChatItem(text.Trim(), CommanderChatSource.LocalTopic));
            Changed?.Invoke();
        }

        public void Cancel()
        {
            if (pending == null) return;
            pending.Trace.Log("cancel", "session_cancel; no fallback");
            ++RequestGeneration;
            pending = null;
            transport.Cancel();
            LastOnlineStatus = "已取消";
            AddError("已取消本次在线通讯。草稿已保留；预设主题仍可使用。");
        }

        // CommanderRuntimeHost forwards settings edits; old callbacks cannot reach fallback.
        public void ConfigurationChanged()
        {
            pending?.Trace.Log("invalidate", "configuration_changed");
            Cancel();
            LastOnlineStatus = "未验证";
            Changed?.Invoke();
        }

        public void Reset()
        {
            pending?.Trace.Log("invalidate", "session_reset_or_dispose; no fallback");
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
            LastOnlineStatus = "未验证";
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
            request.Trace.Log("session.result", "status=" + result.Status + " http=" + result.HttpStatus);
            if (pending != request || request.SessionId != SessionId ||
                request.Generation != RequestGeneration) { request.Trace.Log("discard", "pending/session/generation mismatch; no fallback"); return; }
            DiagnosticTrace = request.Trace;
            pending = null;
            if (!source.IsCurrent(request.Focus))
            {
                request.Trace.Log("discard", "focus_changed; no fallback");
                DropStaleReply();
                return;
            }
            if (result.Status != CommanderTransportStatus.Success)
            {
                if (result.Status == CommanderTransportStatus.Cancelled)
                {
                    LastOnlineStatus = "已取消";
                    AddError("在线通讯已取消。草稿已保留；预设主题仍可使用。");
                    return;
                }
                CompleteFallback(request, TransportFailure(result.Status));
                return;
            }
            if (!CommanderResponseParser.TryParse(result.Content, request.Snapshot, out var parsed, out var parseReason))
            {
                request.Trace.Log("parse.rejected", parseReason);
                CompleteFallback(request, "在线回复格式或支援选项无效；请手动重试。");
                return;
            }
            request.Trace.Log("parse.accepted", JsonConvert.SerializeObject(new { reply = parsed.Reply, kind = parsed.Kind, option_id = parsed.OptionId }));
            Guid? proposalId = null;
            if (parsed.Kind.HasValue)
            {
                var proposal = new CommanderProposal(Guid.NewGuid(), SessionId,
                    request.Generation, request.Snapshot.SnapshotId, parsed.OptionId,
                    parsed.Kind.Value);
                if (!support.TryRegister(proposal, out var reason))
                {
                    request.Trace.Log("support.rejected", reason);
                    CompleteFallback(request, "支援选项已失效。" + (string.IsNullOrWhiteSpace(reason) ?
                        "请重新询问。" : reason));
                    return;
                }
                proposalId = proposal.ProposalId;
                request.Trace.Log("support.registered", "proposal=" + proposalId + "; contract confirmation still required; not executed");
            }
            request.Trace.Log("ui.online", "fallback=false; proposal=" + proposalId + " reply=" + parsed.Reply);
            // Keep assistant history in the same protocol as the requested response.
            // Only validated fields enter history; reasoning prefixes never do.
            string wireAnswer = JsonConvert.SerializeObject(new
            {
                reply = parsed.Reply,
                proposal = parsed.Kind.HasValue ? new
                {
                    kind = parsed.Kind.Value == NativeSupportKind.Medical ? "medical" : "weakpoint",
                    option_id = parsed.OptionId
                } : null
            });
            var turn = new Turn { Question = request.Question, WireAnswer = wireAnswer };
            turns.Add(turn);
            if (turns.Count > 6) turns.RemoveAt(0);
            if (proposalId.HasValue) proposalTurns.Add(proposalId.Value, turn);
            messages.Add(new CommanderChatItem(parsed.Reply, CommanderChatSource.OnlineAssistant, proposalId));
            LastOnlineStatus = "上次请求成功";
            if (string.Equals(draft.Trim(), request.Question, StringComparison.Ordinal))
                draft = string.Empty;
            Changed?.Invoke();
        }

        void CompleteFallback(Pending request, string reason)
        {
            if (pending != null && pending != request) return;
            if (request.SessionId != SessionId || request.Generation != RequestGeneration) { request.Trace.Log("discard", "pending/session/generation mismatch; no fallback"); return; }
            DiagnosticTrace = request.Trace;
            pending = null;
            if (!source.IsCurrent(request.Focus))
            {
                request.Trace.Log("discard", "focus_changed; no fallback");
                DropStaleReply();
                return;
            }
            request.Trace.Log("fallback", "fallback=true; reason=" + reason);
            LastOnlineStatus = "上次在线失败 · 本地可用";
            messages.Add(new CommanderChatItem(reason + " 草稿已保留，可手动重试。",
                CommanderChatSource.Error));
            messages.Add(new CommanderChatItem(
                "本地预设 / 当前任务：" + request.Snapshot.CurrentObjective +
                "\n自由输入没有本地问答模型，不能按任意问题生成回答。请选择下方任务、记忆、身份或授权主题；支援与记录仍可独立使用。",
                CommanderChatSource.LocalFact));
            request.Trace.Log("ui.fallback", "status=" + LastOnlineStatus + "\n" + messages[messages.Count - 2].Text + "\n" + messages[messages.Count - 1].Text);
            Changed?.Invoke();
        }

        void DropStaleReply()
        {
            LastOnlineStatus = "状态已变化";
            AddError("局内进度或焦点已变化，旧请求已丢弃；草稿已保留，请在当前状态下手动重试。");
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
                result.Add(new CommanderMessage(CommanderMessageRole.Assistant, turn.WireAnswer));
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
            DiagnosticTrace?.Log("ui.error", text);
            messages.Add(new CommanderChatItem(text, CommanderChatSource.Error));
            Changed?.Invoke();
        }

        static string TransportFailure(CommanderTransportStatus status)
        {
            switch (status)
            {
                case CommanderTransportStatus.Timeout: return "在线通讯超时。";
                case CommanderTransportStatus.InvalidConfiguration: return "在线配置无效，请检查地址、模型和 Key。";
                case CommanderTransportStatus.InvalidResponse: return "服务回复无效。";
                case CommanderTransportStatus.NetworkError: return "在线网络暂不可达。";
                case CommanderTransportStatus.HttpError: return "在线服务返回错误。";
                default: return "在线通讯暂不可用。";
            }
        }
    }
}
