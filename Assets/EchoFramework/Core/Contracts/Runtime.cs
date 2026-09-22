using System;
using System.Collections.Generic;
namespace Echo.Framework.Contracts
{
    public sealed class Diagnostic
    {
        public string Code { get; } public string File { get; } public string ContentId { get; } public string FieldPath { get; } public string Message { get; }
        public Diagnostic(string code, string file, string contentId, string fieldPath, string message) { Code=code; File=file; ContentId=contentId; FieldPath=fieldPath; Message=message; }
    }
    public sealed class Result<T>
    {
        public bool Succeeded { get; } public T Value { get; } public Diagnostic Error { get; }
        private Result(bool success,T value,Diagnostic error) { Succeeded=success; Value=value; Error=error; }
        public static Result<T> Ok(T value) => new Result<T>(true,value,null);
        public static Result<T> Fail(Diagnostic error) => new Result<T>(false,default,error ?? throw new ArgumentNullException(nameof(error)));
    }
    public readonly struct Unit { public static Unit Value => default; }
    public enum ActionStatus { Running, Succeeded, Failed, Cancelled }
    public enum ExecutionContextKind { Ordinary, Attack, BeforeDamageCommit }
    public enum ClockKind { Simulation, Presentation }
    public readonly struct ClockSample
    {
        public long Tick { get; } public double StepSeconds { get; } public bool SimulationPaused { get; }
        public ClockSample(long tick,double stepSeconds,bool paused) { if(tick<0 || double.IsNaN(stepSeconds)||double.IsInfinity(stepSeconds)||stepSeconds<=0) throw new ArgumentException("Invalid clock"); Tick=tick; StepSeconds=stepSeconds; SimulationPaused=paused; }
    }
    public interface IClock { ClockSample Current { get; } }
    public interface IScopeService
    {
        Result<ScopeRef> Create(ScopeKind kind, ScopeRef? parent);
        bool IsAlive(ScopeRef scope); bool Contains(ScopeRef ancestor, ScopeRef descendant);
        Result<Unit> End(ScopeRef scope,string reason);
        IDisposable OnEnding(ScopeRef scope,Action<string> cleanup);
    }
    public sealed class EventEnvelope
    {
        public string EventId { get; } public string RunId { get; } public long Tick { get; } public long Sequence { get; }
        public string Type { get; } public InstanceRef? Source { get; } public ScopeRef Scope { get; } public InstanceRef? Target { get; }
        public string CausationId { get; } public IReadOnlyDictionary<string,TypedValue> Payload { get; }
        public EventEnvelope(string eventId,string runId,long tick,long sequence,string type,InstanceRef? source,ScopeRef scope,InstanceRef? target,string causationId,IDictionary<string,TypedValue> payload)
        {
            if(string.IsNullOrWhiteSpace(runId)||!scope.Instance.IsValid||string.IsNullOrWhiteSpace(eventId)||string.IsNullOrWhiteSpace(type)||tick<0||sequence<1||runId!=scope.Instance.RunId || (source.HasValue && source.Value.RunId!=runId)||(target.HasValue&&target.Value.RunId!=runId)) throw new ArgumentException("Invalid event envelope");
            EventId=eventId;RunId=runId;Tick=tick;Sequence=sequence;Type=type;Source=source;Scope=scope;Target=target;CausationId=causationId;Payload=Frozen.Map(payload);
        }
    }
    public enum ScopeMatch { Exact, Descendants }
    public sealed class EventFilter
    {
        public string Type { get; } public ScopeRef Scope { get; } public ScopeMatch Match { get; }
        public EventFilter(string type,ScopeRef scope,ScopeMatch match) { Type=type;Scope=scope;Match=match; }
    }
    // Module registration issues a publisher bound to that module's owned fact types.
    public interface IEventPublisher { Result<string> Enqueue(string type,ScopeRef scope,InstanceRef? source,InstanceRef? target,string causationId,IReadOnlyDictionary<string,TypedValue> payload); }
    public interface IEventStream { IDisposable Subscribe(EventFilter filter,Action<EventEnvelope> listener); }
    // Read-only current-run history; reading never republishes facts or invokes actions.
    public interface IFactHistory { IReadOnlyList<EventEnvelope> Read(EventFilter filter,long afterSequence,long throughSequence); long LastSequence { get; } }
    public interface IServiceResolver { T Require<T>() where T:class; }
    public interface IServiceBindings { void Bind<T>(T service) where T:class; }
    public sealed class ExecutionContext
    {
        public InstanceRef Execution { get; } public ScopeRef Scope { get; } public ExecutionContextKind Kind { get; }
        public InstanceRef? Actor { get; } public InstanceRef? Attack { get; } public EventEnvelope Trigger { get; } public IServiceResolver Services { get; }
        public ExecutionContext(InstanceRef execution,ScopeRef scope,ExecutionContextKind kind,InstanceRef? actor,InstanceRef? attack,EventEnvelope trigger,IServiceResolver services)
        { Execution=execution;Scope=scope;Kind=kind;Actor=actor;Attack=attack;Trigger=trigger;Services=services; }
    }
    public sealed class ActionResult
    {
        public ActionStatus Status { get; } public IReadOnlyDictionary<string,TypedValue> Output { get; } public Diagnostic Error { get; }
        public ActionResult(ActionStatus status,IDictionary<string,TypedValue> output,Diagnostic error=null) { if(status==ActionStatus.Failed && error==null)throw new ArgumentException("Failure requires diagnostic"); Status=status;Output=Frozen.Map(output);Error=error; }
    }
    public interface IActionInstance { ActionResult Start(); ActionResult Tick(ClockSample clock); ActionResult Cancel(string reason); }
    public interface IActionFactory { Result<IActionInstance> Create(IReadOnlyDictionary<string,TypedValue> parameters,ExecutionContext context); }
    public interface ICondition { Result<bool> Evaluate(IReadOnlyDictionary<string,TypedValue> parameters,ExecutionContext context); }
    public interface ICommandHandler { Result<IReadOnlyDictionary<string,TypedValue>> Execute(IReadOnlyDictionary<string,TypedValue> parameters,ExecutionContext context); }
    public interface ITraceSink { void Record(TraceEntry entry); }
    public sealed class TraceEntry
    {
        public long Tick { get; } public string RuleId { get; } public InstanceRef Execution { get; } public string CausationId { get; } public string Transition { get; } public string Reason { get; }
        public TraceEntry(long tick,string ruleId,InstanceRef execution,string causationId,string transition,string reason) { Tick=tick;RuleId=ruleId;Execution=execution;CausationId=causationId;Transition=transition;Reason=reason; }
    }
    public interface ILogicModule : IDisposable { string ModuleId { get; } void Initialize(IServiceResolver services); void Tick(ClockSample clock); }
    public interface IContentResolver { Result<T> Resolve<T>(ContentId id) where T:class; }
    public interface IContentTypeReader { Result<object> Read(string validatedJson,ContentId id); }
}
