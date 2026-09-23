using System.Collections.Generic;
using Echo.Framework.Contracts;
using Echo.Gameplay.Contracts;
namespace Echo.Content.Contracts
{
    public enum CaseStepKind { Input, Advance, WaitEvent, MoveToMarker }
    public enum FixtureKind { Unit, Integration }
    public sealed class CaseStep
    {
        public CaseStepKind Kind { get; } public InputIntent Input { get; } public long Ticks { get; }
        public string EventType { get; } public InstanceRef? Actor { get; } public MarkerRef? Marker { get; }
        public CaseStep(CaseStepKind kind,InputIntent input,long ticks,string eventType,InstanceRef? actor,MarkerRef? marker) { Kind=kind;Input=input;Ticks=ticks;EventType=eventType;Actor=actor;Marker=marker; }
    }
    public sealed class CaseExpectation
    {
        public ActionStatus Outcome { get; } public IReadOnlyList<string> ErrorCodes { get; } public IReadOnlyDictionary<string,long> EventCounts { get; }
        public IReadOnlyList<string> EventOrder { get; } public IReadOnlyDictionary<string,long> RemainingResources { get; }
        public CaseExpectation(ActionStatus outcome,IEnumerable<string> errors,IDictionary<string,long> events,IEnumerable<string> order,IDictionary<string,long> resources) { Outcome=outcome;ErrorCodes=Frozen.List(errors);EventCounts=Frozen.Map(events);EventOrder=Frozen.List(order);RemainingResources=Frozen.Map(resources); }
    }
    public sealed class ContentCase
    {
        public string CaseId { get; } public IReadOnlyList<string> RequirementIds { get; } public PackageDependency Package { get; } public ContentId Entrypoint { get; }
        public int Seed { get; } public double Timestep { get; } public long MaxTicks { get; } public FixtureKind FixtureKind { get; } public string FixtureId { get; }
        public IReadOnlyList<CaseStep> Steps { get; } public CaseExpectation Expect { get; } public VerificationLayer Layer { get; }
        public ContentCase(string id,IEnumerable<string> requirements,PackageDependency package,ContentId entrypoint,int seed,double timestep,long maxTicks,FixtureKind fixtureKind,string fixtureId,IEnumerable<CaseStep> steps,CaseExpectation expect,VerificationLayer layer)
        { CaseId=id;RequirementIds=Frozen.List(requirements);Package=package;Entrypoint=entrypoint;Seed=seed;Timestep=timestep;MaxTicks=maxTicks;FixtureKind=fixtureKind;FixtureId=fixtureId;Steps=Frozen.List(steps);Expect=expect;Layer=layer; }
    }
    public sealed class ValidationReport
    {
        public string ReportVersion { get; } public string ContentDigest { get; } public IReadOnlyList<PackageDependency> Dependencies { get; }
        public string FrameworkVersion { get; } public string FrameworkDigest { get; } public string TestedCommit { get; }
        public string CatalogDigest { get; } public string SchemaDigest { get; } public string RunnerVersion { get; }
        public IReadOnlyDictionary<string,string> Environment { get; } public int Seed { get; } public IReadOnlyList<CaseReport> Cases { get; }
        public ValidationReport(string reportVersion,string contentDigest,IEnumerable<PackageDependency> dependencies,string frameworkVersion,string frameworkDigest,string testedCommit,string catalogDigest,string schemaDigest,string runnerVersion,IDictionary<string,string> environment,int seed,IEnumerable<CaseReport> cases)
        { ReportVersion=reportVersion;ContentDigest=contentDigest;Dependencies=Frozen.List(dependencies);FrameworkVersion=frameworkVersion;FrameworkDigest=frameworkDigest;TestedCommit=testedCommit;CatalogDigest=catalogDigest;SchemaDigest=schemaDigest;RunnerVersion=runnerVersion;Environment=Frozen.Map(environment);Seed=seed;Cases=Frozen.List(cases); }
    }
    public interface IContentCaseRunner { CaseReport Run(ContentCase contentCase,ILogicWorld world); }
    public interface IProductionComposition { IReadOnlyList<IModuleDescriptor> Modules { get; } ILogicWorld CreateWorld(); }
}
