using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Echo.Framework.Contracts
{
    // Intentionally bounded Draft-07 profile. This is not a full JSON Schema implementation.
    public static class ContractJson
    {
        public const string Profile = "echo-draft07-subset/0.1.0";
        private static readonly HashSet<string> Keywords = new HashSet<string>(new[]{"$schema","$id","title","description","type","properties","required","additionalProperties","items","minItems","maxItems","minLength","maxLength","minimum","maximum","enum","pattern","oneOf","default"},StringComparer.Ordinal);
        public static JToken Parse(string json)
        {
            // Json.NET extensions (comments, single quotes, constructors, NaN, trailing commas) are forbidden.
            var stripped=Regex.Replace(json,@"""(?:[^""\\\x00-\x1F]|\\(?:[""\\/bfnrt]|u[0-9a-fA-F]{4}))*""","\"\"");
            if(Regex.IsMatch(stripped,@",\s*[}\]]") || Regex.IsMatch(stripped,@"[^\[\]{}:, \t\r\n""0-9eE.+\-a-z]")) throw new FormatException("Non-JSON syntax");
            var lexer=new Regex(@"\G(?:""""|true|false|null|-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?|[\[\]{}:, \t\r\n])");
            for(int offset=0;offset<stripped.Length;)
            {
                var match=lexer.Match(stripped,offset);
                if(!match.Success)throw new FormatException("Non-JSON token");
                offset+=match.Length;
                if((char.IsDigit(match.Value[0])||match.Value[0]=='-') && offset<stripped.Length && ",]} \t\r\n".IndexOf(stripped[offset])<0)throw new FormatException("Invalid JSON number");
            }
            using(var reader=new JsonTextReader(new StringReader(json)) { DateParseHandling=DateParseHandling.None,FloatParseHandling=FloatParseHandling.Double,MaxDepth=64 })
            {
                var token=JToken.Load(reader,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load });
                if(reader.Read())throw new FormatException("Trailing JSON value");
                RejectNonFinite(token); return token;
            }
        }
        private static void RejectNonFinite(JToken token)
        {
            if(token.Type==JTokenType.Comment)throw new FormatException("Comments forbidden");
            if(token.Type==JTokenType.Float && (double.IsNaN((double)token)||double.IsInfinity((double)token)))throw new FormatException("Finite numbers required");
            foreach(var child in token.Children()) RejectNonFinite(child);
        }
        public static IReadOnlyList<Diagnostic> Validate(JObject schema,JToken value,string file,string contentId="")
        {
            CheckSchema(schema,"$"); var errors=new List<Diagnostic>(); Visit(schema,value,"$",file,contentId,errors); return errors.AsReadOnly();
        }
        public static void CheckSchema(JObject schema,string path)
        {
            foreach(var property in schema.Properties()) if(!Keywords.Contains(property.Name))throw new NotSupportedException("Unsupported schema keyword: "+path+"."+property.Name);
            foreach(string key in new[]{"$schema","$id","title","description","type","pattern"}) if(schema[key]!=null && schema[key].Type!=JTokenType.String)throw new FormatException("Schema string required: "+key);
            var type=(string)schema["type"];
            if(type!=null && !new[]{"object","array","string","integer","number","boolean","null"}.Contains(type))throw new NotSupportedException("Unsupported schema type: "+type);
            if(schema["$schema"]!=null && (string)schema["$schema"]!="http://json-schema.org/draft-07/schema#")throw new NotSupportedException("Unsupported schema dialect");
            if(schema["additionalProperties"]!=null && schema["additionalProperties"].Type!=JTokenType.Boolean)throw new NotSupportedException("additionalProperties must be boolean");
            foreach(string key in new[]{"minItems","maxItems","minLength","maxLength"}) if(schema[key]!=null && (schema[key].Type!=JTokenType.Integer || (long)schema[key]<0))throw new FormatException("Invalid schema limit: "+key);
            foreach(string key in new[]{"minimum","maximum"}) if(schema[key]!=null && schema[key].Type!=JTokenType.Integer && schema[key].Type!=JTokenType.Float)throw new FormatException("Invalid numeric schema limit");
            if(schema["pattern"]!=null) _ = new Regex((string)schema["pattern"]);
            if(schema["required"]!=null && (!(schema["required"] is JArray required) || required.Any(x=>x.Type!=JTokenType.String) || required.Select(x=>(string)x).Distinct().Count()!=required.Count))throw new FormatException("Invalid required list");
            if(schema["enum"]!=null && (!(schema["enum"] is JArray en) || en.Count==0 || en.Select(x=>x.ToString(Formatting.None)).Distinct().Count()!=en.Count))throw new FormatException("Invalid enum");
            if(schema["properties"]!=null) { if(!(schema["properties"] is JObject props))throw new FormatException("properties must be object"); foreach(var p in props.Properties()) { if(!(p.Value is JObject child))throw new FormatException("Schema object required");CheckSchema(child,path+".properties."+p.Name); } }
            if(schema["items"]!=null) { if(!(schema["items"] is JObject item))throw new NotSupportedException("Tuple items unsupported");CheckSchema(item,path+".items"); }
            if(schema["oneOf"]!=null) { if(!(schema["oneOf"] is JArray variants)||variants.Count==0)throw new FormatException("oneOf array required"); foreach(var variant in variants) { if(!(variant is JObject obj))throw new FormatException("Schema object required");CheckSchema(obj,path+".oneOf"); } }
        }
        private static void Visit(JObject s,JToken v,string path,string file,string id,List<Diagnostic> errors)
        {
            Action<string,string> fail=(code,msg)=>errors.Add(new Diagnostic(code,file,id,path,msg));
            var type=(string)s["type"];
            bool matches=type==null || (type=="object" && v is JObject)||(type=="array" && v is JArray)||(type=="string"&&v.Type==JTokenType.String)||(type=="integer"&&v.Type==JTokenType.Integer)||(type=="number"&&(v.Type==JTokenType.Integer||v.Type==JTokenType.Float))||(type=="boolean"&&v.Type==JTokenType.Boolean)||(type=="null"&&v.Type==JTokenType.Null);
            if(!matches){fail("schema.type","Expected "+type);return;}
            if(s["enum"] is JArray values && !values.Any(x=>JToken.DeepEquals(x,v)))fail("schema.enum","Value not in enum");
            if(s["oneOf"] is JArray branches)
            {
                int count=0;foreach(JObject branch in branches){var local=new List<Diagnostic>();Visit(branch,v,path,file,id,local);if(local.Count==0)count++;}
                if(count!=1)fail("schema.one_of","Expected exactly one matching variant; got "+count);
            }
            if(v is JObject obj)
            {
                var props=s["properties"] as JObject;
                foreach(var required in (s["required"] as JArray ?? new JArray()))if(obj.Property((string)required)==null)errors.Add(new Diagnostic("schema.required",file,id,path+"."+(string)required,"Required field missing"));
                foreach(var prop in obj.Properties())
                {
                    if(props?[prop.Name] is JObject child)Visit(child,prop.Value,path+"."+prop.Name,file,id,errors);
                    else if((bool?)s["additionalProperties"]==false)errors.Add(new Diagnostic("schema.unknown_field",file,id,path+"."+prop.Name,"Unknown field"));
                }
            }
            if(v is JArray array)
            {
                if(s["minItems"]!=null&&array.Count<(int)s["minItems"])fail("schema.min_items","Too few items");
                if(s["maxItems"]!=null&&array.Count>(int)s["maxItems"])fail("schema.max_items","Too many items");
                if(s["items"] is JObject item)for(int i=0;i<array.Count;i++)Visit(item,array[i],path+"["+i+"]",file,id,errors);
            }
            if(v.Type==JTokenType.String)
            {
                string str=(string)v;int scalarLength=0;
                for(int i=0;i<str.Length;i++){scalarLength++;if(char.IsHighSurrogate(str[i])&&i+1<str.Length&&char.IsLowSurrogate(str[i+1]))i++;}
                if(s["minLength"]!=null&&scalarLength<(int)s["minLength"])fail("schema.min_length","String too short");
                if(s["maxLength"]!=null&&scalarLength>(int)s["maxLength"])fail("schema.max_length","String too long");
                if(s["pattern"]!=null&&!Regex.IsMatch(str,(string)s["pattern"],RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1)))fail("schema.pattern","Invalid string pattern");
            }
            if(v.Type==JTokenType.Integer||v.Type==JTokenType.Float)
            {
                double number=(double)v;
                if(double.IsNaN(number)||double.IsInfinity(number))fail("schema.finite","Finite number required");
                if(s["minimum"]!=null&&number<(double)s["minimum"])fail("schema.minimum","Below minimum");
                if(s["maximum"]!=null&&number>(double)s["maximum"])fail("schema.maximum","Above maximum");
            }
        }
    }
}
