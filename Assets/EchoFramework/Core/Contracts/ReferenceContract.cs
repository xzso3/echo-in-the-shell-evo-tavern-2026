using System;
using System.Collections.Generic;
using System.Linq;
namespace Echo.Framework.Contracts
{
    public sealed class PackageMetadata
    {
        public PackageManifest Manifest { get; } public string Sha256 { get; } public IReadOnlyList<ContentId> Definitions { get; }
        public PackageMetadata(PackageManifest manifest,string digest,IEnumerable<ContentId> definitions) { Manifest=manifest;Sha256=digest;Definitions=Frozen.List(definitions); }
    }
    // Metadata-only contract guards. P1-03 still owns filesystem loading, hashing and full package validation.
    public static class ReferenceContract
    {
        public static IReadOnlyList<Diagnostic> CheckPackages(IReadOnlyList<PackageMetadata> packages)
        {
            var errors=new List<Diagnostic>();var byId=new Dictionary<string,PackageMetadata>(StringComparer.Ordinal);
            foreach(var p in packages)
            {
                if(byId.ContainsKey(p.Manifest.PackageId))errors.Add(Error("package.duplicate",p.Manifest.PackageId,"$.package_id"));else byId.Add(p.Manifest.PackageId,p);
                if(p.Definitions.Distinct().Count()!=p.Definitions.Count)errors.Add(Error("content.duplicate",p.Manifest.PackageId,"$.definitions"));
                if(p.Definitions.Any(id=>id.Package!=p.Manifest.PackageId))errors.Add(Error("content.owner",p.Manifest.PackageId,"$.definitions"));
                if(p.Manifest.Dependencies.Select(d=>d.PackageId).Distinct().Count()!=p.Manifest.Dependencies.Count)errors.Add(Error("dependency.duplicate",p.Manifest.PackageId,"$.dependencies"));
                foreach(var export in p.Manifest.Exports)if(export.Package!=p.Manifest.PackageId||!p.Definitions.Contains(export))errors.Add(Error("content.export",export.Value,"$.exports"));
                foreach(var entry in p.Manifest.Entrypoints)if(entry.Type!="level"||entry.Package!=p.Manifest.PackageId||!p.Definitions.Contains(entry))errors.Add(Error("content.entrypoint",entry.Value,"$.entrypoints"));
            }
            foreach(var p in packages)foreach(var d in p.Manifest.Dependencies)
            {
                if(!byId.TryGetValue(d.PackageId,out var target))errors.Add(Error("dependency.missing",p.Manifest.PackageId,"$.dependencies"));
                else if(d.Version!=target.Manifest.PackageVersion)errors.Add(Error("dependency.version",p.Manifest.PackageId,"$.dependencies"));
                else if(d.Sha256!=target.Sha256)errors.Add(Error("dependency.digest",p.Manifest.PackageId,"$.dependencies"));
            }
            var visiting=new HashSet<string>();var visited=new HashSet<string>();
            Action<string> walk=null;walk=id=> { if(visiting.Contains(id)){errors.Add(Error("dependency.cycle",id,"$.dependencies"));return;}if(!visited.Add(id)||!byId.ContainsKey(id))return;visiting.Add(id);foreach(var dep in byId[id].Manifest.Dependencies)walk(dep.PackageId);visiting.Remove(id); };
            foreach(var id in byId.Keys)walk(id);return errors.AsReadOnly();
        }
        public static Result<ContentId> Resolve(PackageMetadata source,ContentId reference,string expectedType,IReadOnlyList<PackageMetadata> packages)
        {
            string error=null;
            if(reference.Type!=expectedType)error="reference.type";
            var targets=packages.Where(p=>p.Manifest.PackageId==reference.Package).ToArray();
            if(error==null && targets.Length!=1)error=targets.Length==0?"reference.missing_package":"package.duplicate";
            if(error==null)
            {
                var target=targets[0];
                if(reference.Package!=source.Manifest.PackageId)
                {
                    var deps=source.Manifest.Dependencies.Where(d=>d.PackageId==reference.Package).ToArray();
                    if(deps.Length!=1)error="reference.undeclared_dependency";
                    else if(deps[0].Version!=target.Manifest.PackageVersion||deps[0].Sha256!=target.Sha256)error="reference.dependency_mismatch";
                    else if(!target.Manifest.Exports.Contains(reference))error="reference.private";
                }
                if(error==null&&!target.Definitions.Contains(reference))error="reference.missing";
            }
            return error==null?Result<ContentId>.Ok(reference):Result<ContentId>.Fail(Error(error,reference.Value,"$.reference"));
        }
        private static Diagnostic Error(string code,string id,string path)=>new Diagnostic(code,"manifest.json",id,path,"Contract metadata rejected: "+code);
    }
}
