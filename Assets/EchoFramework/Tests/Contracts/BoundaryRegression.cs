using System;
using System.Collections.Generic;
using System.Numerics;
using Echo.Framework.Contracts;
using Newtonsoft.Json.Linq;
namespace Echo.Tests.Contracts
{
    // INT-00 R1/R2/R3 regressions. Same source executes in both hosts.
    public static class BoundaryRegression
    {
        public static void Run(Action<string,Action,string> check)
        {
            var schemas=SchemaCatalog.Build();
            var patterns=new Dictionary<string,(JObject schema,string valid)>
            {
                ["content_id"]=(SchemaCatalog.Id(),"p:actor/a"),
                ["resource_id"]=((JObject)schemas["resources"]["properties"]["resources"]["items"]["properties"]["id"],"p:resource/a"),
                ["package_id"]=(SchemaCatalog.PackageId(),"p.sample"),
                ["resource_path"]=(SchemaCatalog.Path(),"assets/a.png"),
                ["version"]=(SchemaCatalog.Version(),"0.1.0"),
                ["digest"]=(SchemaCatalog.Digest(),new string('a',64))
            };
            foreach(var pair in patterns)
            {
                check("R1.full_string."+pair.Key,()=>
                {
                    Accept(pair.Value.schema,new JValue(pair.Value.valid));
                    foreach(var suffix in new[]{"\n","\r","\r\n","\t"," ","\0","\u2028","\u2029"})
                    { Reject(pair.Value.schema,new JValue(pair.Value.valid+suffix));Reject(pair.Value.schema,new JValue(suffix+pair.Value.valid)); }
                },"Accept valid lexical value; reject leading/trailing control characters and whitespace");
            }
            check("R1.content_id_constructor",()=>
            {
                ContractSmoke.Assert(new ContentId("p:actor/a").Value=="p:actor/a","Valid ID rejected");
                foreach(var suffix in new[]{"\n","\r","\r\n","\t"," ","\0","\u2028","\u2029","/"})
                    Throws<ArgumentException>(()=>new ContentId("p:actor/a"+suffix));
            },"Runtime ContentId matches strict schema boundary");
            foreach(var key in new[]{"minLength","maxLength","minItems","maxItems"})
            {
                check("R2.limit_range."+key,()=>
                {
                    foreach(var invalid in new JToken[]{new JValue(-1),new JValue(2147483648L),JToken.Parse("9223372036854775808"),JToken.Parse("999999999999999999999999999999"),new JValue(0.5),new JValue("1")})
                    {
                        var s=new JObject{[key]=invalid};
                        Throws<FormatException>(()=>ContractJson.CheckSchema(s,"$"));
                        Throws<FormatException>(()=>ContractJson.Validate(s,new JValue("a"),"regression"));
                    }
                    var maximum=new JObject{[key]=int.MaxValue};ContractJson.CheckSchema(maximum,"$");
                    // Exercise casts at the legal endpoint, including min limits that should diagnose a short value.
                    JToken value=key.EndsWith("Items")?(JToken)new JArray():new JValue("a");
                    var errors=ContractJson.Validate(maximum,value,"regression");
                    ContractSmoke.Assert(key.StartsWith("min")?errors.Count==1:errors.Count==0,"Incorrect Int32 endpoint behavior");
                    ContractJson.CheckSchema(new JObject{[key]=0},"$");
                },"Count bounds must be 0..Int32.MaxValue; schema rejection is FormatException, never overflow");
            }
            foreach(var key in new[]{"minimum","maximum"})
                check("R2.finite_schema."+key,()=>
                {
                    foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
                    {
                        var s=new JObject{["type"]="number",[key]=invalid};
                        Throws<FormatException>(()=>ContractJson.CheckSchema(s,"$"));
                        Throws<FormatException>(()=>ContractJson.Validate(s,new JValue(1),"regression"));
                    }
                    var valid=new JObject{["type"]="number",[key]=key=="minimum"?-double.MaxValue:double.MaxValue};
                    Accept(valid,new JValue(0));
                },"Reject NaN and both infinities in schema bounds; accept finite extrema");
            check("R2.inverted_bounds",()=>
            {
                foreach(var pair in new[]{("minimum","maximum"),("minLength","maxLength"),("minItems","maxItems")})
                    Throws<FormatException>(()=>ContractJson.CheckSchema(new JObject{[pair.Item1]=2,[pair.Item2]=1},"$"));
            },"Reject inverted numeric and count ranges before validating content");
            check("R2.nonfinite_nested_annotation",()=>Throws<FormatException>(()=>ContractJson.CheckSchema(new JObject{["type"]="object",["default"]=new JObject{["value"]=double.NaN}},"$")),"Programmatic schema trees must also contain finite JSON numbers");
            check("R2.exact_integer_comparison",()=>
            {
                var upper=new JObject{["type"]="integer",["maximum"]=long.MaxValue};
                Accept(upper,new JValue(long.MaxValue));Reject(upper,JToken.Parse("9223372036854775808"));
                var lower=new JObject{["type"]="integer",["minimum"]=long.MinValue};
                Accept(lower,new JValue(long.MinValue));Reject(lower,JToken.Parse("-9223372036854775809"));
                Reject(upper,new JValue(BigInteger.Pow(10,400)));
                var exact=new JObject{["type"]="integer",["minimum"]=9007199254740993L};
                Reject(exact,new JValue(9007199254740992L));Accept(exact,new JValue(9007199254740993L));
            },"Compare integer tokens exactly, including BigInteger and adjacent values beyond double precision");
            check("R2.mixed_number_comparison",()=>
            {
                Reject(new JObject{["maximum"]=9007199254740992d},new JValue(9007199254740993L));
                Accept(new JObject{["maximum"]=9007199254740992d},new JValue(9007199254740992L));
                Reject(new JObject{["minimum"]=9007199254740993L},new JValue(9007199254740992d));
                Reject(new JObject{["maximum"]=0.5},new JValue(1));
                Accept(new JObject{["minimum"]=0.5},new JValue(1));
                Reject(new JObject{["minimum"]=-0.5},new JValue(-1));
                Accept(new JObject{["maximum"]=-0.5},new JValue(-1));
                Reject(new JObject{["maximum"]=1},new JValue(1.5));
                Reject(new JObject{["minimum"]=-1},new JValue(-1.5));
            },"Mixed integer/float comparisons preserve boundary and fractional sign");
            check("R2.nonfinite_content",()=>
            {
                foreach(double n in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})Reject(new JObject{["type"]="number"},new JValue(n));
                Reject(new JObject{["type"]="number"},new JValue(BigInteger.Pow(10,400)));
            },"Programmatic nonfinite content produces a diagnostic");
            var int32Fields=new Dictionary<string,JObject>
            {
                ["generation"]=(JObject)schemas["instance_ref"]["properties"]["generation"],
                ["case_seed"]=(JObject)schemas["case"]["properties"]["seed"],
                ["report_seed"]=(JObject)schemas["report"]["properties"]["seed"],
                ["module_order"]=(JObject)schemas["module_descriptor"]["properties"]["order"]
            };
            foreach(var pair in int32Fields)
                check("R3.int32."+pair.Key,()=>
                {
                    long minimum=pair.Key=="generation"?1:0;
                    Accept(pair.Value,new JValue(minimum));Accept(pair.Value,new JValue(int.MaxValue));
                    Reject(pair.Value,new JValue(minimum-1));Reject(pair.Value,new JValue(2147483648L));
                    Reject(pair.Value,JToken.Parse("9223372036854775808"));Reject(pair.Value,new JValue(1.5));
                    ContractSmoke.Assert((int)int.MaxValue==new InstanceRef("r",InstanceKind.Entity,1,int.MaxValue).Generation,"DTO endpoint differs");
                },"Wire integer fits DTO Int32; legal endpoints accepted, neighboring and huge values rejected");
            check("R3.wire_int64_safe_range",()=>
            {
                var wire=SchemaCatalog.Integer();Accept(wire,new JValue(9007199254740991L));
                Reject(wire,new JValue(9007199254740992L));Reject(wire,new JValue(long.MaxValue));
                var signed=SchemaCatalog.Integer(-9007199254740991L);Accept(signed,new JValue(-9007199254740991L));Reject(signed,new JValue(-9007199254740992L));
            },"Existing Int64 wire fields remain within exact JSON safe integer policy");
        }
        private static void Accept(JObject schema,JToken value) => ContractSmoke.Assert(ContractJson.Validate(schema,value,"boundary-regression").Count==0,"Valid boundary rejected: "+value);
        private static void Reject(JObject schema,JToken value) => ContractSmoke.Assert(ContractJson.Validate(schema,value,"boundary-regression").Count>0,"Invalid boundary accepted: "+value);
        private static void Throws<T>(Action action) where T:Exception
        { try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name); }
    }
}
