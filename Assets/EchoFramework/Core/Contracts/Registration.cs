using System;
using System.Collections.Generic;
namespace Echo.Framework.Contracts
{
    public enum CapabilityKind { Event, Condition, Action, Command, Hook, ContentType }
    public enum CapabilityTiming { Instant, Sustained, Composite }
    public enum RegistrationUse { Production, ContractFixture }
    // SchemaJson contains a schema from the supported profile, not executable expressions.
    public sealed class CapabilityDescriptor
    {
        public string Id { get; } public string Version { get; } public CapabilityKind Kind { get; } public string OwnerModule { get; }
        public string ParameterSchemaJson { get; } public string ResultSchemaJson { get; }
        public IReadOnlyList<ScopeKind> AllowedScopes { get; } public IReadOnlyList<ExecutionContextKind> Contexts { get; }
        public CapabilityTiming Timing { get; } public string Cancellation { get; } public string SideEffects { get; }
        public IReadOnlyList<string> HookPermissions { get; } public IReadOnlyList<string> EmittedEvents { get; } public IReadOnlyList<string> ErrorCodes { get; }
        public RegistrationUse Use { get; }
        public CapabilityDescriptor(string id,string version,CapabilityKind kind,string ownerModule,string parameterSchemaJson,string resultSchemaJson,IEnumerable<ScopeKind> allowedScopes,IEnumerable<ExecutionContextKind> contexts,CapabilityTiming timing,string cancellation,string sideEffects,IEnumerable<string> hookPermissions,IEnumerable<string> emittedEvents,IEnumerable<string> errorCodes,RegistrationUse use)
        { Id=id;Version=version;Kind=kind;OwnerModule=ownerModule;ParameterSchemaJson=parameterSchemaJson;ResultSchemaJson=resultSchemaJson;AllowedScopes=Frozen.List(allowedScopes);Contexts=Frozen.List(contexts);Timing=timing;Cancellation=cancellation;SideEffects=sideEffects;HookPermissions=Frozen.List(hookPermissions);EmittedEvents=Frozen.List(emittedEvents);ErrorCodes=Frozen.List(errorCodes);Use=use; }
    }
    public interface IModuleDescriptor
    {
        string Id { get; } string Version { get; } int Order { get; }
        IReadOnlyList<string> Dependencies { get; }
        void Register(ICapabilityRegistry registry);
        ILogicModule Create(IServiceResolver services);
    }
    // Concrete handler required; a descriptor alone is never an implemented capability.
    public interface ICapabilityRegistry
    {
        void RegisterAction(CapabilityDescriptor descriptor,IActionFactory factory);
        void RegisterCondition(CapabilityDescriptor descriptor,ICondition condition);
        void RegisterCommand(CapabilityDescriptor descriptor,ICommandHandler handler);
        void RegisterEvent(CapabilityDescriptor descriptor);
        void RegisterContentType(CapabilityDescriptor descriptor,IContentTypeReader reader);
        void RegisterHook(CapabilityDescriptor descriptor,IHookHandler handler);
        IReadOnlyList<CapabilityDescriptor> Export(RegistrationUse use);
        void Seal();
    }
    // Core does not know gameplay damage. Hook candidate type and access are owned by the module.
    public interface IHookCandidate { string HookId { get; } }
    public interface IHookHandler { Result<Unit> Apply(IHookCandidate candidate,ExecutionContext context,IReadOnlyDictionary<string,TypedValue> parameters); }
    public sealed class StartupRequest
    {
        public string RunId { get; } public ContentId EntryPoint { get; } public int Seed { get; } public double Timestep { get; }
        public StartupRequest(string runId,ContentId entryPoint,int seed,double timestep) { RunId=runId;EntryPoint=entryPoint;Seed=seed;Timestep=timestep; }
    }
}
