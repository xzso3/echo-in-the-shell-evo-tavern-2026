using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace Echo.Framework.Contracts
{
    public static class DescriptorJson
    {
        public static JObject Export(CapabilityDescriptor d)
        {
            var parameters=(JObject)ContractJson.Parse(d.ParameterSchemaJson);var result=(JObject)ContractJson.Parse(d.ResultSchemaJson);
            ContractJson.CheckSchema(parameters,"$.parameter_schema");ContractJson.CheckSchema(result,"$.result_schema");
            var json=new JObject { ["id"]=d.Id,["version"]=d.Version,["kind"]=Wire(d.Kind.ToString()),["owner_module"]=d.OwnerModule,["parameter_schema"]=parameters,["result_schema"]=result,["allowed_scopes"]=new JArray(d.AllowedScopes.Select(x=>Wire(x.ToString()))),["contexts"]=new JArray(d.Contexts.Select(x=>Wire(x.ToString()))),["timing"]=Wire(d.Timing.ToString()),["cancellation"]=d.Cancellation,["side_effects"]=d.SideEffects,["hook_permissions"]=new JArray(d.HookPermissions),["emitted_events"]=new JArray(d.EmittedEvents),["error_codes"]=new JArray(d.ErrorCodes),["use"]=Wire(d.Use.ToString()) };
            var errors=ContractJson.Validate(SchemaCatalog.Build()["capability_descriptor"],json,"registration",d.Id);
            if(errors.Count>0)throw new ArgumentException(errors[0].FieldPath+": "+errors[0].Message);
            return json;
        }
        public static string Wire(string value) => System.Text.RegularExpressions.Regex.Replace(value,@"(?<!^)([A-Z])","_$1").ToLowerInvariant();
        public static Result<Unit> CheckUse(CapabilityDescriptor d,string version,ScopeKind scope,ExecutionContextKind context,RegistrationUse use)
        {
            string code = d.Version!=version ? "capability.version" : d.Use!=use ? "capability.unavailable" : !d.AllowedScopes.Contains(scope) ? "capability.scope" : !d.Contexts.Contains(context) ? "capability.context" : context==ExecutionContextKind.BeforeDamageCommit && (d.Timing!=CapabilityTiming.Instant||d.HookPermissions.Count==0) ? "capability.hook" : null;
            return code==null?Result<Unit>.Ok(Unit.Value):Result<Unit>.Fail(new Diagnostic(code,"registration",d.Id,"$.capability","Capability is incompatible with requested use"));
        }
    }
}
