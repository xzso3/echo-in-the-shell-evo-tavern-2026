using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Echo.Framework.Contracts;
using Echo.Gameplay.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Echo.Tests.Contracts
{
    // Tests only: no fixture handler or ActorDouble is a production module.
    public static class ContractSmoke
    {
        public static JObject Run(string root,string host,string testedCommit)
        {
            var cases=new JArray();var schemas=SchemaCatalog.Build();
            Action<string,Action,string> check=(id,test,expected)=>
            {
                var clock=Stopwatch.StartNew();string status="passed",actual="Expected assertions observed";var diagnostics=new JArray();
                try{test();}catch(Exception error){status="failed";actual=error.ToString();diagnostics.Add(new JObject{["code"]="test.assertion",["file"]="ContractSmoke.cs",["content_id"]="",["field_path"]="$",["message"]=error.Message});}
                cases.Add(new JObject{["case_id"]=id,["status"]=status,["duration_ms"]=clock.Elapsed.TotalMilliseconds,["expected"]=expected,["actual"]=actual,["diagnostics"]=diagnostics,["artifacts"]=new JArray()});
            };
            foreach(var path in Directory.GetFiles(Path.Combine(root,"Tests/EchoFramework/Contracts/Fixtures"),"*.json").OrderBy(x=>x,StringComparer.Ordinal))
            {
                var f=(JObject)ContractJson.Parse(File.ReadAllText(path));
                check((string)f["case_id"],()=> { var errors=ContractJson.Validate(schemas[(string)f["schema"]],f["data"],path,(string)f["data"]?["package_id"]??"");string expected=(string)f["expected_code"];Assert(expected=="" ? errors.Count==0 : errors.Any(x=>x.Code==expected && x.File==path && x.FieldPath.StartsWith("$")),"Expected diagnostic "+expected+"; got "+string.Join(",",errors.Select(x=>x.Code+":"+x.FieldPath))); },"Schema fixture: "+((string)f["expected_code"]==""?"accept":(string)f["expected_code"]));
            }
            check("A01.no_engine_dependency",()=> { foreach(var assembly in new[]{typeof(ContentId).Assembly,typeof(IActorControl).Assembly}) Assert(!assembly.GetReferencedAssemblies().Any(a=>a.Name.StartsWith("Unity")||a.Name.Contains("Plague")),"Engine/prototype reference"); },"Core and Gameplay contain no engine or prototype assembly reference");
            check("A01.json_version",()=> { var version=FileVersionInfo.GetVersionInfo(typeof(JObject).Assembly.Location).ProductVersion;Assert(version.StartsWith("13.0.2"),"JSON version "+version); },"Newtonsoft.Json 13.0.2");
            foreach(string keyword in new[]{"$ref","allOf","anyOf","not","if","format","patternProperties","unevaluatedProperties"})
                check("A03.unsupported_schema_"+keyword,()=>Throws<NotSupportedException>(()=>ContractJson.Validate(new JObject{[keyword]=true},new JObject(),"fixture")),"Reject unsupported schema keyword");
            foreach(var pair in new Dictionary<string,string>{{"duplicate","{\"a\":1,\"a\":2}"},{"comment","{/*bad*/\"a\":1}"},{"trailing_comma","{\"a\":1,}"},{"nonfinite","{\"a\":NaN}"},{"single_quote","{'a':1}"},{"extra_value","{} {}"},{"overflow","{\"a\":1e999}"}})
                check("A03.json_"+pair.Key,()=>Throws<Exception>(()=>ContractJson.Parse(pair.Value)),"Reject nonstandard or ambiguous JSON");
            check("A01.json_numeric_forms",()=> { Assert((double)ContractJson.Parse("1e-01")==0.1,"Valid exponent rejected");Assert((double)ContractJson.Parse("0.001")==0.001,"Valid decimal rejected");Throws<Exception>(()=>ContractJson.Parse("01")); },"JSON decimal and exponent forms accepted; leading zero rejected");
            check("A03.unsupported_nested_schema",()=>Throws<NotSupportedException>(()=>ContractJson.Validate(new JObject{["oneOf"]=new JArray(new JObject{["type"]="string"},new JObject{["type"]="object",["properties"]=new JObject{["x"]=new JObject{["format"]="email"}}})},new JValue("selected other branch"),"fixture")),"Reject unsupported schema keyword even in unselected branch");
            check("A01.unicode_schema_length",()=>Assert(ContractJson.Validate(new JObject{["type"]="string",["maxLength"]=1},new JValue("😀"),"fixture").Count==0,"UTF-16 length used"),"Length uses Unicode scalar count");
            var descriptor=FixtureDescriptor();
            check("A02.descriptor_generation",()=> { var exported=DescriptorJson.Export(descriptor);Assert(JToken.DeepEquals(exported["parameter_schema"],ContractJson.Parse(descriptor.ParameterSchemaJson)),"Schema divergence");Assert((string)exported["use"]=="contract_fixture","Fixture mislabeled"); },"Descriptor produces same schema and fixture designation");
            check("A03.fixture_not_production",()=>Assert(!DescriptorJson.CheckUse(descriptor,"0.1.0",ScopeKind.Execution,ExecutionContextKind.Ordinary,RegistrationUse.Production).Succeeded,"Fixture leaked"),"Fixture is unavailable to production");
            check("A03.unknown_capability",()=>Assert(FindFixture("combat.execute_attack")==null,"Planned capability available"),"Only actual fixture descriptor available to fixture probe");
            check("A05.scope",()=>Assert(DescriptorJson.CheckUse(descriptor,"0.1.0",ScopeKind.Run,ExecutionContextKind.Ordinary,RegistrationUse.ContractFixture).Error.Code=="capability.scope","Scope accepted"),"Reject wrong scope");
            check("A05.context",()=>Assert(DescriptorJson.CheckUse(descriptor,"0.1.0",ScopeKind.Execution,ExecutionContextKind.Attack,RegistrationUse.ContractFixture).Error.Code=="capability.context","Context accepted"),"Reject wrong execution context");
            check("A05.attack_only_contract_context",()=> { var d=new CapabilityDescriptor("fixture.fire","0.1.0",CapabilityKind.Action,"contract.fixture","{}","{}",new[]{ScopeKind.Execution},new[]{ExecutionContextKind.Attack},CapabilityTiming.Instant,"no pending work","fixture only",new string[0],new string[0],new string[0],RegistrationUse.ContractFixture);Assert(DescriptorJson.CheckUse(d,"0.1.0",ScopeKind.Execution,ExecutionContextKind.Ordinary,RegistrationUse.ContractFixture).Error.Code=="capability.context","Attack-only action accepted outside attack context"); },"Contract fixture for attack-only command rejects ordinary context");
            check("A05.hook_sustained",()=> { var d=new CapabilityDescriptor("fixture.hook","0.1.0",CapabilityKind.Action,"fixture","{}","{}",new[]{ScopeKind.Execution},new[]{ExecutionContextKind.BeforeDamageCommit},CapabilityTiming.Sustained,"bounded","none",new[]{"amount"},new string[0],new string[0],RegistrationUse.ContractFixture);Assert(DescriptorJson.CheckUse(d,"0.1.0",ScopeKind.Execution,ExecutionContextKind.BeforeDamageCommit,RegistrationUse.ContractFixture).Error.Code=="capability.hook","Sustained hook accepted"); },"Sustained hook rejected");
            check("A05.identity_generation",()=> { var old=new InstanceRef("run",InstanceKind.Entity,1,1);var replacement=new InstanceRef("run",InstanceKind.Entity,1,2);Assert(!old.Equals(replacement),"Generation lost");Assert(!old.Equals(new InstanceRef("other",InstanceKind.Entity,1,1)),"Run identity lost"); },"Generation and run participate in equality");
            check("A05.invalid_ids",()=> { Throws<ArgumentException>(()=>new ContentId("untyped"));Throws<ArgumentException>(()=>new ContentId("p:actor/../x"));Throws<ArgumentException>(()=>TypedValue.From(double.NaN));Throws<ArgumentException>(()=>new ScopeRef(new InstanceRef("r",InstanceKind.Entity,1),ScopeKind.Level)); },"Reject malformed content, nonfinite value and scope mismatch");
            check("A02.immutable_event",()=> { var p=new Dictionary<string,TypedValue>{{"value",TypedValue.From(1L)}};var e=new EventEnvelope("event","r",0,1,"fixture.event",null,new ScopeRef(new InstanceRef("r",InstanceKind.Level,1),ScopeKind.Level),null,null,p);p["value"]=TypedValue.From(2L);Assert((long)e.Payload["value"].Value==1,"Payload changed after submit"); },"Envelope copies submitted payload");
            check("A05.actor_flow_contract_double",()=>ActorConsumerCheck(),"06-style consumer compiles against shared 05 services; double verifies lease, stale and death expectations only");
            check("A04.marker_instance_context",()=> { var m=new InstanceRef("r",InstanceKind.Map,1);var a=new MarkerRef(m,new InstanceRef("r",InstanceKind.Chunk,2),"spawn");var b=new MarkerRef(m,new InstanceRef("r",InstanceKind.Chunk,3),"spawn");Assert(!a.Chunk.Equals(b.Chunk),"Local marker alias");Throws<ArgumentException>(()=>new MarkerRef(m,new InstanceRef("other",InstanceKind.Chunk,2),"spawn")); },"Map/Chunk context is explicit and same-run");
            MetadataChecks(check);
            BoundaryRegression.Run(check);
            var source=SourceDigest(root);var catalog=new JObject{["contract_version"]=ContractVersion.Current,["use"]="production",["capabilities"]=new JArray()};
            var report=new JObject{["report_version"]=ContractVersion.Current,["content_digest"]=TreeDigest(root,new[]{"Tests/EchoFramework/Contracts/Fixtures"}),["dependency_digests"]=new JArray(),["framework_version"]=ContractVersion.Current,["framework_digest"]=source,["tested_commit"]=testedCommit,["catalog_digest"]=Hash(catalog.ToString(Formatting.None)),["schema_digest"]=Hash(string.Join("\n",schemas.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>x.Key+":"+x.Value.ToString(Formatting.None)))),["runner_version"]=ContractVersion.Current,["environment"]=new JObject{["host"]=host,["runtime"]=Environment.Version.ToString(),["os"]=Environment.OSVersion.ToString()},["seed"]=1,["cases"]=cases};
            Assert(ContractJson.Validate(schemas["report"],report,"report").Count==0,"Generated report violates schema");return report;
        }
        public static CapabilityDescriptor FixtureDescriptor() => new CapabilityDescriptor("fixture.wait","0.1.0",CapabilityKind.Action,"contract.fixture",SchemaCatalog.FixtureWaitParameters().ToString(Formatting.None),SchemaCatalog.Object().ToString(Formatting.None),new[]{ScopeKind.Execution},new[]{ExecutionContextKind.Ordinary},CapabilityTiming.Sustained,"cancel before next tick; release test lease","fixture only; no production state",new string[0],new string[0],new[]{"fixture.cancelled"},RegistrationUse.ContractFixture);
        private static CapabilityDescriptor FindFixture(string id) { var d=FixtureDescriptor();return d.Id==id?d:null; }
        public static string SourceDigest(string root) => TreeDigest(root,new[]{"Assets/EchoFramework","Assets/EchoFramework.meta","Tests/EchoFramework/Host","Tests/EchoFramework/Contracts","Tools/EchoContent/Contracts","global.json","Directory.Build.props","EchoFramework.sln",".gitignore"});
        public static string TreeDigest(string root,IEnumerable<string> paths)
        {
            var files=paths.SelectMany(p=>File.Exists(Path.Combine(root,p))?new[]{Path.Combine(root,p)}:Directory.GetFiles(Path.Combine(root,p),"*",SearchOption.AllDirectories)).Where(p=>!p.Replace('\\','/').Contains("/obj/")&&!p.Replace('\\','/').Contains("/bin/")).OrderBy(x=>x,StringComparer.Ordinal);
            return Hash(string.Join("\n",files.Select(p=>p.Substring(root.Length).Replace('\\','/')+":"+Hash(File.ReadAllBytes(p)))));
        }
        public static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
        public static string Hash(byte[] value) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","").ToLowerInvariant(); }
        public static void Export(string root)
        {
            var directory=Path.Combine(root,"Docs/Framework/Contracts/Generated");Directory.CreateDirectory(directory);
            foreach(var pair in SchemaCatalog.Build()){var path=Path.Combine(directory,pair.Key+".schema.json");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,pair.Value.ToString(Formatting.Indented)+"\n");}
            File.WriteAllText(Path.Combine(directory,"production-catalog.json"),new JObject{["contract_version"]=ContractVersion.Current,["use"]="production",["capabilities"]=new JArray()}.ToString(Formatting.Indented)+"\n");
            File.WriteAllText(Path.Combine(directory,"fixture-catalog.json"),new JObject{["contract_version"]=ContractVersion.Current,["use"]="contract_fixture",["capabilities"]=new JArray(DescriptorJson.Export(FixtureDescriptor()))}.ToString(Formatting.Indented)+"\n");
        }
        public static void Assert(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
        private static void Throws<T>(Action action) where T:Exception { try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name); }

        private static void MetadataChecks(Action<string,Action,string> check)
        {
            PackageMetadata Package(string id,string version="0.1.0",PackageDependency[] deps=null,bool exported=true) { var cid=new ContentId(id+":level/main");return new PackageMetadata(new PackageManifest(id,version,"0.1.0",new CapabilityRequirement[0],deps??new PackageDependency[0],exported?new[]{cid}:new ContentId[0],new[]{cid},"resources.json"),new string('0',64),new[]{cid}); }
            var b=Package("dep");var a=Package("fixture",deps:new[]{new PackageDependency("dep","0.1.0",b.Sha256)});var all=new[]{a,b};
            check("A02.metadata_reference",()=>Assert(ReferenceContract.CheckPackages(all).Count==0&&ReferenceContract.Resolve(a,new ContentId("dep:level/main"),"level",all).Succeeded,"Valid reference failed"),"Valid dependency/export/type resolves using metadata");
            check("A04.duplicate_package",()=>Assert(ReferenceContract.CheckPackages(new[]{a,a,b}).Any(d=>d.Code=="package.duplicate"),"Duplicate accepted"),"Reject duplicate package ID");
            check("A04.missing_dependency",()=>Assert(ReferenceContract.CheckPackages(new[]{a}).Any(d=>d.Code=="dependency.missing"),"Missing accepted"),"Reject missing dependency");
            check("A04.dependency_version",()=>Assert(ReferenceContract.CheckPackages(new[]{a,Package("dep","0.2.0")}).Any(d=>d.Code=="dependency.version"),"Wrong version accepted"),"Reject wrong exact dependency version");
            check("A04.dependency_cycle",()=> { var aa=Package("a",deps:new[]{new PackageDependency("b","0.1.0",b.Sha256)});var bb=Package("b",deps:new[]{new PackageDependency("a","0.1.0",b.Sha256)});Assert(ReferenceContract.CheckPackages(new[]{aa,bb}).Any(d=>d.Code=="dependency.cycle"),"Cycle accepted"); },"Reject metadata dependency cycle");
            check("A04.private_reference",()=>Assert(ReferenceContract.Resolve(a,new ContentId("dep:level/main"),"level",new[]{a,Package("dep",exported:false)}).Error.Code=="reference.private","Private export accepted"),"Reject unexported reference");
            check("A04.wrong_reference_type",()=>Assert(ReferenceContract.Resolve(a,new ContentId("dep:level/main"),"actor",all).Error.Code=="reference.type","Wrong type accepted"),"Reject reference type mismatch");
            check("A04.undeclared_dependency",()=>Assert(ReferenceContract.Resolve(Package("fixture"),new ContentId("dep:level/main"),"level",all).Error.Code=="reference.undeclared_dependency","Undeclared accepted"),"Reject undeclared cross-package reference");
        }
        private static void ActorConsumerCheck()
        {
            var fake=new ActorDouble();IActorQuery query=fake;IActorControl control=fake;var owner=new ScopeRef(new InstanceRef("r",InstanceKind.Execution,2),ScopeKind.Execution);
            Assert(query.Get(fake.Actor).Value.Alive,"Not alive");
            var first=control.TryAcquire(new ControlRequest(fake.Actor,owner,ControlChannel.Interaction|ControlChannel.Movement,true,"dialogue"));Assert(first.Succeeded,"Lease unavailable");
            Assert(query.Get(fake.Actor).Value.Busy,"Busy missing");Assert(!query.CanUse(fake.Actor,ControlChannel.Movement,null).Value,"Movement not blocked");Assert(query.CanUse(fake.Actor,ControlChannel.Movement,first.Value.Id).Value,"Owner denied");
            Assert(!control.TryAcquire(new ControlRequest(fake.Actor,owner,ControlChannel.Movement,true,"overlap")).Succeeded,"Overlap accepted");
            first.Value.Dispose();first.Value.Dispose();Assert(control.ActiveLeaseCount==0&&!query.Get(fake.Actor).Value.Busy,"Lease not released idempotently");
            control.TryAcquire(new ControlRequest(fake.Actor,owner,ControlChannel.Interaction,true,"dialogue"));control.ReleaseOwner(owner);Assert(control.ActiveLeaseCount==0,"Scope cleanup missing");
            Assert(!query.Get(new InstanceRef("r",InstanceKind.Entity,1,2)).Succeeded,"Stale reference rebound");fake.Alive=false;Assert(!control.TryAcquire(new ControlRequest(fake.Actor,owner,ControlChannel.Interaction,true,"dead")).Succeeded,"Dead actor acquired lease");
        }
        private sealed class ActorDouble : IActorQuery,IActorControl
        {
            public readonly InstanceRef Actor=new InstanceRef("r",InstanceKind.Entity,1);public bool Alive=true;private readonly List<Lease> leases=new List<Lease>();private long serial=10;
            public int ActiveLeaseCount=>leases.Count(l=>l.IsActive);
            public Result<ActorState> Get(InstanceRef actor) => !actor.Equals(Actor)?Result<ActorState>.Fail(Error("entity.stale")):Result<ActorState>.Ok(new ActorState(Actor,Alive,leases.Any(l=>l.IsActive&&l.Busy),leases.Where(l=>l.IsActive).Aggregate(ControlChannel.None,(a,l)=>a|l.Channels),"idle",null,false,null,0.25,Alive?10:0,"test",true));
            public Result<bool> CanUse(InstanceRef actor,ControlChannel channels,InstanceRef? lease) { var state=Get(actor);return !state.Succeeded?Result<bool>.Fail(state.Error):Result<bool>.Ok(Alive&&!leases.Any(l=>l.IsActive&&(l.Channels&channels)!=0&&(!lease.HasValue||!l.Id.Equals(lease.Value)))); }
            public Result<IControlLease> TryAcquire(ControlRequest request) { var can=CanUse(request.Actor,request.Channels,null);if(!can.Succeeded||!can.Value)return Result<IControlLease>.Fail(Error("actor.unavailable"));var l=new Lease(new InstanceRef("r",InstanceKind.ControlLease,serial++),request);leases.Add(l);return Result<IControlLease>.Ok(l); }
            public Result<Unit> Release(InstanceRef id) { foreach(var l in leases.Where(l=>l.Id.Equals(id)))l.Dispose();return Result<Unit>.Ok(Unit.Value); }
            public void ReleaseOwner(ScopeRef owner){foreach(var l in leases.Where(l=>l.Owner.Equals(owner)))l.Dispose();}
            private static Diagnostic Error(string code)=>new Diagnostic(code,"ActorDouble","","$","Contract double rejection");
            private sealed class Lease:IControlLease
            {
                public InstanceRef Id{get;}public InstanceRef Actor{get;}public ScopeRef Owner{get;}public ControlChannel Channels{get;}public bool Busy{get;}public bool IsActive{get;private set;}=true;
                public Lease(InstanceRef id,ControlRequest r){Id=id;Actor=r.Actor;Owner=r.Owner;Channels=r.Channels;Busy=r.MarksBusy;}
                public void Dispose(){IsActive=false;}
            }
        }
    }
}
