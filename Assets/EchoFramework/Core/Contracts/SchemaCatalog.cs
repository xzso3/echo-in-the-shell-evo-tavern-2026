using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace Echo.Framework.Contracts
{
    // Single schema description source. Generated files are outputs, never hand-maintained inputs.
    public static class SchemaCatalog
    {
        public static JObject Text(string pattern=null) { var s=new JObject { ["type"]="string",["minLength"]=1 }; if(pattern!=null)s["pattern"]=pattern;return s; }
        public static JObject Enum(params string[] values) => new JObject { ["type"]="string",["enum"]=new JArray(values) };
        public static JObject Integer(long min=0) => new JObject { ["type"]="integer",["minimum"]=min,["maximum"]=9007199254740991L };
        public static JObject Number(double min=0) => new JObject { ["type"]="number",["minimum"]=min };
        public static JObject Bool() => new JObject { ["type"]="boolean" };
        public static JObject List(JObject items,int min=0) => new JObject { ["type"]="array",["items"]=items,["minItems"]=min };
        public static JObject Object(params (string name,JObject schema)[] fields) => ObjectOptional(new string[0],fields);
        public static JObject ObjectOptional(string[] optional,params (string name,JObject schema)[] fields)
        { var props=new JObject();var required=new JArray();foreach(var field in fields){props[field.name]=field.schema; if(!optional.Contains(field.name))required.Add(field.name);}return new JObject{["type"]="object",["additionalProperties"]=false,["properties"]=props,["required"]=required}; }
        public static JObject FixtureWaitParameters()
        {
            var seconds=Number();seconds["description"]="Simulation seconds; zero yields at least one tick";seconds["default"]=0;
            return Object(("duration",seconds),("clock",Enum("simulation")));
        }
        public static JObject Version() => Text(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$");
        public static JObject Digest() => Text("^[a-f0-9]{64}$");
        public static JObject Id() => Text(ContentId.Pattern);
        public static JObject Path() => Text(@"^(?!/)(?!.*(?:^|/)\.\.(?:/|$))(?!.*\\)[a-zA-Z0-9_./-]+$");
        public static JObject Ref(params string[] kind) => Object(("run_id",Text()),("kind",Enum(kind.Length==0?new[]{"run","level","encounter","entity","execution","rule_set","attack","quest","dialogue","map","chunk","projectile","control_lease"}:kind)),("serial",Integer(1)),("generation",Integer(1)));
        public static JObject Scope() => Ref("run","level","encounter","entity","execution");
        public static JObject Point() => Object(("x",new JObject{["type"]="number"}),("y",new JObject{["type"]="number"}));
        public static JObject Nullable(JObject value) => new JObject{["oneOf"]=new JArray(value,new JObject{["type"]="null"})};
        public static JObject TypedValue() => new JObject { ["oneOf"]=new JArray(new[]{("boolean",Bool()),("integer",Integer(-9007199254740991L)),("number",new JObject{["type"]="number"}),("string",new JObject{["type"]="string"}),("content_id",Id()),("instance_ref",Ref())}.Select(x=>Object(("kind",Enum(x.Item1)),("value",x.Item2)))) };
        public static JObject Diagnostic() => Object(("code",Text()),("file",Text()),("content_id",new JObject{["type"]="string"}),("field_path",Text()),("message",Text()));
        public static JObject PayloadFields() => List(Object(("name",Text()),("value",TypedValue())));
        public static JObject Event(JObject payload) => ObjectOptional(new[]{"source_ref","target_ref","causation_id"},("event_id",Text()),("run_id",Text()),("tick",Integer()),("sequence",Integer(1)),("type",Text()),("source_ref",Ref()),("scope_ref",Scope()),("target_ref",Ref()),("causation_id",Text()),("payload",payload));
        public static JObject Input()
        {
            JObject Base(string kind,params (string,JObject)[] fields) => Object(new[]{("input_id",Text()),("run_id",Text()),("sequence",Integer(1)),("tick",Integer()),("kind",Enum(kind))}.Concat(fields).ToArray());
            return new JObject{["oneOf"]=new JArray(Base("move",("actor",Ref("entity")),("direction",Point())),Base("toggle_auto_fire",("actor",Ref("entity"))),Base("interact",("actor",Ref("entity")),("target",Ref("entity")),("interaction_id",Text())),Base("dialogue_advance",("session",Ref("dialogue")),("node_id",Text()),("revision",Integer())),Base("dialogue_choose",("session",Ref("dialogue")),("node_id",Text()),("revision",Integer()),("choice_id",Text())),Base("dialogue_close",("session",Ref("dialogue")),("node_id",Text()),("revision",Integer())))};
        }
        public static IReadOnlyDictionary<string,JObject> Build()
        {
            var result=new SortedDictionary<string,JObject>(StringComparer.Ordinal);
            result["package"]=Object(("package_id",Text("^[a-z][a-z0-9_.-]*$")),("package_version",Version()),("schema_version",Enum(ContractVersion.Current)),("required_capabilities",List(Object(("id",Text()),("version",Version())))),("dependencies",List(Object(("package_id",Text()),("package_version",Version()),("sha256",Digest())))),("exports",List(Id())),("entrypoints",List(Id())),("resource_manifest",Path()));
            result["resources"]=Object(("schema_version",Enum(ContractVersion.Current)),("resources",List(Object(("id",Text(@"^(?!.*\.\.)(?!.*\/$)[a-z][a-z0-9_.-]*:resource/[a-z][a-z0-9_./-]*$")),("type",Enum("sprite","texture","audio","text","font")),("source_path",Path()),("sha256",Digest()),("license",Text())))));
            result["input"]=Input();result["instance_ref"]=Ref();result["typed_value"]=TypedValue();
            result["case"]=Object(("case_id",Text()),("requirement_ids",List(Text(),1)),("package",Object(("package_id",Text()),("package_version",Version()),("sha256",Digest()))),("entrypoint",Id()),("seed",Integer(0)),("timestep",new JObject{["type"]="number",["minimum"]=0.000001,["maximum"]=1}), ("max_ticks",Integer(1)),("setup",Object(("fixture_kind",Enum("unit","integration")),("fixture_id",Text()))),("steps",List(new JObject{["oneOf"]=new JArray(Object(("op",Enum("input")),("intent",Input())),Object(("op",Enum("advance")),("ticks",Integer(1))),Object(("op",Enum("wait_event")),("event_type",Text()),("max_ticks",Integer(1))),Object(("op",Enum("move_to_marker")),("actor",Ref("entity")),("marker",Object(("map",Ref("map")),("chunk",Ref("chunk")),("local_id",Text()))),("max_ticks",Integer(1))))})),("expect",Object(("outcome",Enum("succeeded","failed","cancelled")),("error_codes",List(Text())),("events",List(Object(("type",Text()),("count",Integer())))),("event_order",List(Text())),("remaining_resources",Object(("listeners",Integer()),("actions",Integer()),("control_leases",Integer()),("projectiles",Integer()),("dialogues",Integer()))))),("verification_layer",Enum("static","logic","unity","manual")));
            result["report"]=Object(("report_version",Enum(ContractVersion.Current)),("content_digest",Digest()),("dependency_digests",List(Object(("package_id",Text()),("sha256",Digest())))),("framework_version",Version()),("framework_digest",Digest()),("tested_commit",Text()),("catalog_digest",Digest()),("schema_digest",Digest()),("runner_version",Version()),("environment",Object(("host",Text()),("runtime",Text()),("os",Text()))),("seed",Integer()),("cases",List(Object(("case_id",Text()),("status",Enum("passed","failed","not_run","blocked")),("duration_ms",Number()),("expected",Text()),("actual",Text()),("diagnostics",List(Diagnostic())),("artifacts",List(Text()))))));

            result["world_snapshot"]=Object(("run_id",Text()),("tick",Integer()),
                ("entities",List(Object(("entity",Ref("entity")),("definition",Id()),("scope",Scope()),("map",Ref("map")),("position",Point()),("facing",Point()),("animation_semantic",Text()),("attack_phase",Nullable(Text())),("phase_progress",new JObject{["type"]="number",["minimum"]=0,["maximum"]=1}),("presentation",Id()),("alive",Bool()),("auto_fire",Bool())))),
                ("doors",List(Object(("door",Ref("entity")),("open",Bool())))),
                ("objectives",List(Object(("quest",Ref("quest")),("objective_id",Text()),("progress",Integer()),("required",Integer()),("state",Enum("inactive","active","completed","failed","cancelled"))))),
                ("interactions",List(Object(("actor",Ref("entity")),("target",Ref("entity")),("interaction_id",Text()),("text",Text()),("available",Bool()),("reason",Nullable(Text()))))),
                ("dialogues",List(Object(("session",Ref("dialogue")),("node_id",Text()),("revision",Integer()),("speaker",Text()),("text",Text()),("choices",List(Object(("choice_id",Text()),("text",Text()))))))),
                ("result",Nullable(Object(("result_id",Text()),("success",Bool()),("reason",Text())))));
            result["capability_descriptor"]=Object(("id",Text()),("kind",Enum("event","condition","action","command","hook","content_type")),("version",Version()),("parameter_schema",ObjectSchema()),("result_schema",ObjectSchema()),("owner_module",Text()),("allowed_scopes",List(Enum("run","level","encounter","entity","execution"),1)),("contexts",List(Enum("ordinary","attack","before_damage_commit"),1)),("timing",Enum("instant","sustained","composite")),("cancellation",Text()),("side_effects",Text()),("hook_permissions",List(Text())),("emitted_events",List(Text())),("error_codes",List(Text())),("use",Enum("production","contract_fixture")));
            result["module_descriptor"]=Object(("id",Text()),("version",Version()),("order",Integer()),("dependencies",List(Text())),("capabilities",List(result["capability_descriptor"])));
            result["fixture_action"]=Object(("id",Text()),("version",Version()),("scope",Enum("run","level","encounter","entity","execution")),("context",Enum("ordinary","attack","before_damage_commit")),("parameters",FixtureWaitParameters()));
            foreach(var pair in PlannedEventPayloads()) { var envelope=Event(pair.Value);envelope["properties"]["type"]=Enum(pair.Key);result["events/"+pair.Key]=envelope; }
            foreach(var pair in result){pair.Value["$schema"]="http://json-schema.org/draft-07/schema#";pair.Value["$id"]="urn:echo:contracts:0.1.0:"+pair.Key;ContractJson.CheckSchema(pair.Value,"$");}
            return result;
        }
        private static JObject ObjectSchema() => new JObject{["type"]="object"}; // Recursively checked by CheckSchema at registration.
        public static IReadOnlyDictionary<string,JObject> PlannedEventPayloads()
        {
            var e=new SortedDictionary<string,JObject>(StringComparer.Ordinal);
            e["level.started"]=Object(("level_instance",Ref("level")));
            foreach(var n in new[]{"level.completed","level.failed"})e[n]=Object(("result_id",Text()),("reason",Text()));
            foreach(var n in new[]{"map.region_entered","map.region_exited"})e[n]=Object(("actor_ref",Ref("entity")),("region_ref",Object(("map",Ref("map")),("local_id",Text()))));
            e["map.door_state_changed"]=Object(("door_ref",Ref("entity")),("old_state",Enum("open","closed")),("new_state",Enum("open","closed")));
            e["actor.target_changed"]=Object(("actor_ref",Ref("entity")),("old_target",Nullable(Ref("entity"))),("new_target",Nullable(Ref("entity"))));
            e["actor.decision_requested"]=Object(("actor_ref",Ref("entity")),("decision_sequence",Integer(1)));
            e["actor.state_changed"]=Object(("actor_ref",Ref("entity")),("old_state",Text()),("new_state",Text()));
            e["actor.died"]=Object(("actor_ref",Ref("entity")),("damage_result_id",Text()),("position",Point()));
            e["combat.attack_started"]=Object(("attack_instance",Ref("attack")),("owner",Ref("entity")),("attack_definition",Id()));
            e["combat.attack_phase_changed"]=Object(("attack_instance",Ref("attack")),("phase",Text()),("progress_origin",Integer()));
            e["combat.attack_finished"]=Object(("attack_instance",Ref("attack")),("result",Enum("succeeded","failed","cancelled")));
            e["combat.damage_committed"]=Object(("result_id",Text()),("source",Ref("entity")),("target",Ref("entity")),("actual_damage",Number()));
            e["interaction.completed"]=Object(("result_id",Text()),("actor",Ref("entity")),("target",Ref("entity")),("interaction_id",Text()));
            foreach(var n in new[]{"dialogue.completed","dialogue.cancelled"})e[n]=Object(("session_id",Ref("dialogue")),("dialogue_ref",Id()),("reason",Text()));
            e["quest.objective_changed"]=Object(("quest_instance",Ref("quest")),("objective_id",Text()),("progress",Integer()),("state",Enum("inactive","active","completed","failed","cancelled")));
            foreach(var n in new[]{"quest.completed","quest.failed"})e[n]=Object(("quest_instance",Ref("quest")),("result_id",Text()),("reason",Text()));
            return e;
        }
    }
}
