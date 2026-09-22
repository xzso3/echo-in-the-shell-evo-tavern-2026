using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
namespace Echo.Framework.Contracts
{
    public static class ContractVersion { public const string Current = "0.1.0"; }
    public static class Frozen
    {
        public static IReadOnlyList<T> List<T>(IEnumerable<T> source) => new List<T>(source ?? throw new ArgumentNullException(nameof(source))).AsReadOnly();
        public static IReadOnlyDictionary<string,T> Map<T>(IDictionary<string,T> source) => new ReadOnlyDictionary<string,T>(new Dictionary<string,T>(source, StringComparer.Ordinal));
    }
    public readonly struct ContentId : IEquatable<ContentId>
    {
        public const string Pattern = @"^(?!.*\.\.)(?!.*\/$)[a-z][a-z0-9_.-]*:[a-z][a-z0-9_]*\/[a-z][a-z0-9_./-]*$";
        public string Value { get; }
        public string Package => Value.Split(':')[0];
        public string Type => Value.Substring(Value.IndexOf(':')+1).Split('/')[0];
        public ContentId(string value) { if (value == null || !Regex.IsMatch(value, Pattern) || value.Contains("..") || value.EndsWith("/")) throw new ArgumentException("Invalid content ID", nameof(value)); Value=value; }
        public bool Equals(ContentId other) => StringComparer.Ordinal.Equals(Value,other.Value);
        public override bool Equals(object other) => other is ContentId id && Equals(id);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? "";
    }
    public enum InstanceKind { Run, Level, Encounter, Entity, Execution, RuleSet, Attack, Quest, Dialogue, Map, Chunk, Projectile, ControlLease }
    public readonly struct InstanceRef : IEquatable<InstanceRef>
    {
        public string RunId { get; }
        public InstanceKind Kind { get; }
        public long Serial { get; }
        public int Generation { get; }
        public InstanceRef(string runId, InstanceKind kind, long serial, int generation = 1)
        { if (string.IsNullOrWhiteSpace(runId) || serial < 1 || generation < 1 || !Enum.IsDefined(typeof(InstanceKind),kind)) throw new ArgumentException("Invalid instance reference"); RunId=runId; Kind=kind; Serial=serial; Generation=generation; }
        public bool IsValid => RunId != null && Serial > 0 && Generation > 0;
        public bool Equals(InstanceRef other) => RunId==other.RunId && Kind==other.Kind && Serial==other.Serial && Generation==other.Generation;
        public override bool Equals(object other) => other is InstanceRef r && Equals(r);
        public override int GetHashCode() => HashCode.Combine(RunId,Kind,Serial,Generation);
        public override string ToString() => $"{RunId}/{Kind}/{Serial}@{Generation}";
    }
    public enum ScopeKind { Run, Level, Encounter, Entity, Execution }
    public readonly struct ScopeRef : IEquatable<ScopeRef>
    {
        public InstanceRef Instance { get; }
        public ScopeKind Kind { get; }
        public ScopeRef(InstanceRef instance, ScopeKind kind)
        { if (!instance.IsValid || instance.Kind.ToString()!=kind.ToString()) throw new ArgumentException("Scope kind mismatch"); Instance=instance; Kind=kind; }
        public bool Equals(ScopeRef other) => Instance.Equals(other.Instance) && Kind==other.Kind;
        public override bool Equals(object other) => other is ScopeRef s && Equals(s);
        public override int GetHashCode() => Instance.GetHashCode();
    }
    public enum ValueKind { Boolean, Integer, Number, String, ContentId, InstanceRef, Object, Array }
    public sealed class TypedValue
    {
        public ValueKind Kind { get; }
        public object Value { get; }
        private TypedValue(ValueKind kind, object value) { Kind=kind; Value=value; }
        // Containers are event/parameter aggregates; variable declarations allow only scalar kinds.
        public static TypedValue Object(IDictionary<string,TypedValue> value) => new TypedValue(ValueKind.Object,Frozen.Map(value));
        public static TypedValue Array(IEnumerable<TypedValue> value) => new TypedValue(ValueKind.Array,Frozen.List(value));
        public static TypedValue From(bool v) => new TypedValue(ValueKind.Boolean,v);
        public static TypedValue From(long v) => new TypedValue(ValueKind.Integer,v);
        public static TypedValue From(double v) { if (double.IsNaN(v)||double.IsInfinity(v)) throw new ArgumentException("Finite number required"); return new TypedValue(ValueKind.Number,v); }
        public static TypedValue From(string v) => new TypedValue(ValueKind.String,v ?? throw new ArgumentNullException(nameof(v)));
        public static TypedValue From(ContentId v) { if (v.Value==null) throw new ArgumentException("Invalid ID"); return new TypedValue(ValueKind.ContentId,v); }
        public static TypedValue From(InstanceRef v) { if (!v.IsValid) throw new ArgumentException("Invalid reference"); return new TypedValue(ValueKind.InstanceRef,v); }
    }
}
