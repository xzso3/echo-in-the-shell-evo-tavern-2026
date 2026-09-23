using System.Collections.Generic;
namespace Echo.Framework.Contracts
{
    public enum ReentryPolicy { Ignore, Restart, Queue, Parallel }
    public enum BindingSource { Literal, EventPayload, BoundActor, BoundScope, Variable }
    public sealed class ValueBinding
    {
        public BindingSource Source { get; } public string Path { get; } public TypedValue Literal { get; }
        public ValueBinding(BindingSource source,string path,TypedValue literal=null) { Source=source;Path=path;Literal=literal; }
    }
    public sealed class ActionDefinition
    {
        public string CapabilityId { get; } public string Version { get; } public IReadOnlyDictionary<string,ValueBinding> Parameters { get; } public IReadOnlyList<ActionDefinition> Children { get; }
        public ActionDefinition(string id,string version,IDictionary<string,ValueBinding> parameters,IEnumerable<ActionDefinition> children) { CapabilityId=id;Version=version;Parameters=Frozen.Map(parameters);Children=Frozen.List(children); }
    }
    public enum ConditionOperator { Leaf, All, Any, Not }
    public sealed class ConditionDefinition
    {
        public ConditionOperator Operator { get; } public string CapabilityId { get; } public string Version { get; } public IReadOnlyDictionary<string,ValueBinding> Parameters { get; } public IReadOnlyList<ConditionDefinition> Children { get; }
        public ConditionDefinition(ConditionOperator op,string id,string version,IDictionary<string,ValueBinding> parameters,IEnumerable<ConditionDefinition> children) { Operator=op;CapabilityId=id;Version=version;Parameters=Frozen.Map(parameters);Children=Frozen.List(children); }
    }
    public sealed class VariableDeclaration
    {
        public string Name { get; } public ValueKind Kind { get; } public ScopeKind Scope { get; } public TypedValue DefaultValue { get; }
        public VariableDeclaration(string name,ValueKind kind,ScopeKind scope,TypedValue defaultValue) { Name=name;Kind=kind;Scope=scope;DefaultValue=defaultValue; }
    }
    public sealed class RuleDefinition
    {
        public ContentId Id { get; } public ScopeKind BindingScope { get; } public string EventType { get; } public ScopeMatch EventScopeMatch { get; }
        public int Priority { get; } public ReentryPolicy Reentry { get; } public int Capacity { get; } public ConditionDefinition Condition { get; } public ActionDefinition Action { get; }
        public RuleDefinition(ContentId id,ScopeKind bindingScope,string eventType,ScopeMatch match,int priority,ReentryPolicy reentry,int capacity,ConditionDefinition condition,ActionDefinition action)
        { Id=id;BindingScope=bindingScope;EventType=eventType;EventScopeMatch=match;Priority=priority;Reentry=reentry;Capacity=capacity;Condition=condition;Action=action; }
    }
    public interface IVariableStore { Result<TypedValue> Read(ScopeRef scope,string name); Result<Unit> Write(ScopeRef scope,string name,TypedValue value); }
    public interface IRuleRuntime { Result<InstanceRef> Bind(RuleDefinition definition,ScopeRef owner,InstanceRef? actor); Result<Unit> Unbind(InstanceRef ruleSetInstance,string reason); }
}
