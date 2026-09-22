using System.Collections.Generic;
namespace Echo.Framework.Contracts
{
    public sealed class PackageDependency
    {
        public string PackageId { get; } public string Version { get; } public string Sha256 { get; }
        public PackageDependency(string id,string version,string sha256) { PackageId=id;Version=version;Sha256=sha256; }
    }
    public sealed class CapabilityRequirement
    {
        public string Id { get; } public string Version { get; }
        public CapabilityRequirement(string id,string version) { Id=id;Version=version; }
    }
    public sealed class PackageManifest
    {
        public string PackageId { get; } public string PackageVersion { get; } public string SchemaVersion { get; }
        public IReadOnlyList<CapabilityRequirement> RequiredCapabilities { get; } public IReadOnlyList<PackageDependency> Dependencies { get; }
        public IReadOnlyList<ContentId> Exports { get; } public IReadOnlyList<ContentId> Entrypoints { get; } public string ResourceManifest { get; }
        public PackageManifest(string id,string version,string schemaVersion,IEnumerable<CapabilityRequirement> capabilities,IEnumerable<PackageDependency> dependencies,IEnumerable<ContentId> exports,IEnumerable<ContentId> entrypoints,string resources)
        { PackageId=id;PackageVersion=version;SchemaVersion=schemaVersion;RequiredCapabilities=Frozen.List(capabilities);Dependencies=Frozen.List(dependencies);Exports=Frozen.List(exports);Entrypoints=Frozen.List(entrypoints);ResourceManifest=resources; }
    }
    public enum CheckStatus { Passed, Failed, NotRun, Blocked }
    public enum VerificationLayer { Static, Logic, Unity, Manual }
    public interface IContentValidator { IReadOnlyList<Diagnostic> Validate(string packageRoot,RegistrationUse use); }
    public interface ICaseRunner { Result<CaseReport> Run(string validatedCaseJson); }
    public sealed class CaseReport
    {
        public string CaseId { get; } public CheckStatus Status { get; } public double DurationMilliseconds { get; }
        public string Expected { get; } public string Actual { get; } public IReadOnlyList<Diagnostic> Diagnostics { get; } public IReadOnlyList<string> Artifacts { get; }
        public CaseReport(string id,CheckStatus status,double duration,string expected,string actual,IEnumerable<Diagnostic> diagnostics,IEnumerable<string> artifacts)
        { CaseId=id;Status=status;DurationMilliseconds=duration;Expected=expected;Actual=actual;Diagnostics=Frozen.List(diagnostics);Artifacts=Frozen.List(artifacts); }
    }
}
