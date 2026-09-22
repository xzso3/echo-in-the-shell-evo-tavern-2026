using System;
using System.Collections.Generic;
using Echo.Framework.Contracts;
using Newtonsoft.Json.Linq;
class Program
{
 static readonly JArray Results = new JArray();
 static void Check(string id, Action test)
 {
  try { test(); Results.Add(new JObject { ["id"]=id,["status"]="passed" }); }
  catch(Exception e) { Results.Add(new JObject { ["id"]=id,["status"]="failed",["actual"]=e.GetType().Name+": "+e.Message }); }
 }
 static void Require(bool value,string message) { if(!value) throw new Exception(message); }
 static bool Rejects(Action action) { try { action(); return false; } catch(ArgumentException) {return true;} catch(FormatException) {return true;} catch(NotSupportedException) {return true;} }
 static int Main()
 {
  Check("control.valid_content_id",()=>Require(new ContentId("p:actor/a").Value=="p:actor/a","valid ID rejected"));
  Check("control.valid_resource_path",()=>Require(ContractJson.Validate(SchemaCatalog.Path(),new JValue("a.png"),"probe").Count==0,"valid path rejected"));
  Check("control.unknown_nested_keyword",()=>Require(Rejects(()=>ContractJson.CheckSchema(new JObject{["oneOf"]=new JArray(new JObject{["type"]="string"},new JObject{["format"]="email"})},"$")),"unknown nested keyword accepted"));
  Check("R1.content_id_terminal_LF",()=>Require(Rejects(()=>{_ = new ContentId("p:actor/a\n");}),"ContentId accepts trailing LF"));
  Check("R1.resource_path_terminal_LF",()=>Require(ContractJson.Validate(SchemaCatalog.Path(),new JValue("a.png\n"),"probe").Count>0,"path schema accepts trailing LF"));
  Check("R1.version_terminal_LF",()=>Require(ContractJson.Validate(SchemaCatalog.Version(),new JValue("0.1.0\n"),"probe").Count>0,"version schema accepts trailing LF"));
  Check("R1.digest_terminal_LF",()=>Require(ContractJson.Validate(SchemaCatalog.Digest(),new JValue(new string('a',64)+"\n"),"probe").Count>0,"digest schema accepts trailing LF"));
  Check("R2.schema_limit_validate_without_overflow",()=>{
   var schema=new JObject{["type"]="string",["maxLength"]=2147483648L};
   if(Rejects(()=>ContractJson.CheckSchema(schema,"$")))return;
   Require(ContractJson.Validate(schema,new JValue("a"),"probe").Count==0,"short string fails large maximum");
  });
  Check("R2.nonfinite_schema_limit_rejected",()=>Require(Rejects(()=>ContractJson.CheckSchema(new JObject{["type"]="number",["minimum"]=double.NaN},"$")),"CheckSchema accepts NaN minimum"));
  Check("R3.generation_fits_runtime_Int32",()=>{
   var value=new JObject{["run_id"]="r",["kind"]="entity",["serial"]=1,["generation"]=2147483648L};
   Require(ContractJson.Validate(SchemaCatalog.Ref(),value,"probe").Count>0,"generation accepted above InstanceRef.Generation Int32 range");
  });
  Check("R3.case_seed_fits_runtime_Int32",()=>{
   var schema=(JObject)SchemaCatalog.Build()["case"]["properties"]["seed"];
   Require(ContractJson.Validate(schema,new JValue(2147483648L),"probe").Count>0,"seed accepted above ContentCase.Seed Int32 range");
  });
  Console.WriteLine(Results.ToString());
  foreach(JObject item in Results)if((string)item["status"]=="failed")return 1;
  return 0;
 }
}
