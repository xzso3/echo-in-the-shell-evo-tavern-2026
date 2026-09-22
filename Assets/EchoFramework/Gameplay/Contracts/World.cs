using System;
using System.Collections.Generic;
using Echo.Framework.Contracts;
namespace Echo.Gameplay.Contracts
{
    public readonly struct Point2
    {
        public double X { get; } public double Y { get; }
        public Point2(double x,double y) { if(double.IsNaN(x)||double.IsNaN(y)||double.IsInfinity(x)||double.IsInfinity(y)) throw new ArgumentException("Finite coordinates required"); X=x;Y=y; }
    }
    [Flags] public enum Blocking { None=0, Movement=1, Projectile=2, Sight=4 }
    public readonly struct Bounds2
    {
        public Point2 Min { get; } public Point2 Max { get; }
        public Bounds2(Point2 min,Point2 max) { if(min.X>max.X||min.Y>max.Y)throw new ArgumentException("Invalid bounds"); Min=min;Max=max; }
    }
    public sealed class SpatialHit
    {
        public bool Hit { get; } public Point2 Position { get; } public Point2 Normal { get; } public double Fraction { get; } public InstanceRef? Blocker { get; }
        public SpatialHit(bool hit,Point2 position,Point2 normal,double fraction,InstanceRef? blocker) { Hit=hit;Position=position;Normal=normal;Fraction=fraction;Blocker=blocker; }
    }
    public readonly struct MarkerRef
    {
        public InstanceRef Map { get; } public InstanceRef Chunk { get; } public string LocalId { get; }
        public MarkerRef(InstanceRef map,InstanceRef chunk,string localId) { if(map.Kind!=InstanceKind.Map||chunk.Kind!=InstanceKind.Chunk||map.RunId!=chunk.RunId||string.IsNullOrWhiteSpace(localId))throw new ArgumentException("Invalid marker context"); Map=map;Chunk=chunk;LocalId=localId; }
    }
    public interface ISpatialQuery
    {
        long Revision { get; }
        Result<SpatialHit> SweepCircle(InstanceRef map,Point2 from,Point2 displacement,double radius,Blocking mask,InstanceRef? ignore);
        Result<bool> HasLineOfSight(InstanceRef map,Point2 from,Point2 to,InstanceRef? ignore);
        Result<IReadOnlyList<Point2>> FindPath(InstanceRef map,Point2 from,Point2 to,double radius);
        Result<Point2> ResolveMarker(MarkerRef marker);
    }
    public interface IDynamicBlocking { Result<Unit> Set(InstanceRef entity,InstanceRef map,Bounds2 bounds,Blocking mask); Result<Unit> Remove(InstanceRef entity); }
    public sealed class EntityState
    {
        public InstanceRef Entity { get; } public ContentId Definition { get; } public ScopeRef Scope { get; } public InstanceRef Map { get; } public Point2 Position { get; }
        public EntityState(InstanceRef entity,ContentId definition,ScopeRef scope,InstanceRef map,Point2 position) { Entity=entity;Definition=definition;Scope=scope;Map=map;Position=position; }
    }
    public interface IEntityQuery { Result<EntityState> Get(InstanceRef entity); bool Exists(InstanceRef entity); IReadOnlyList<EntityState> InScope(ScopeRef scope); }
    public interface IEntityCommands { Result<InstanceRef> Spawn(ContentId definition,ScopeRef owner,InstanceRef map,Point2 position); Result<Unit> Move(InstanceRef entity,Point2 position); Result<Unit> Remove(InstanceRef entity,string reason); }
    [Flags] public enum ControlChannel { None=0, Movement=1, Attack=2, Interaction=4 }
    public sealed class ActorState
    {
        public InstanceRef Actor { get; } public bool Alive { get; } public bool Busy { get; } public ControlChannel Occupied { get; }
        public string StateId { get; } public InstanceRef? Target { get; } public bool TargetVisible { get; } public Point2? LastSeenPosition { get; }
        public double Radius { get; } public double Health { get; } public string Faction { get; } public bool AutoFire { get; }
        public ActorState(InstanceRef actor,bool alive,bool busy,ControlChannel occupied,string stateId,InstanceRef? target,bool targetVisible,Point2? lastSeenPosition,double radius,double health,string faction,bool autoFire)
        { Actor=actor;Alive=alive;Busy=busy;Occupied=occupied;StateId=stateId;Target=target;TargetVisible=targetVisible;LastSeenPosition=lastSeenPosition;Radius=radius;Health=health;Faction=faction;AutoFire=autoFire; }
    }
    // Implemented by P1-05. P1-06 consumes only these shared services.
    public interface IActorQuery { Result<ActorState> Get(InstanceRef actor); Result<bool> CanUse(InstanceRef actor,ControlChannel channels,InstanceRef? lease); }
    public sealed class ControlRequest
    {
        public InstanceRef Actor { get; } public ScopeRef Owner { get; } public ControlChannel Channels { get; } public bool MarksBusy { get; } public string Reason { get; }
        public ControlRequest(InstanceRef actor,ScopeRef owner,ControlChannel channels,bool marksBusy,string reason) { Actor=actor;Owner=owner;Channels=channels;MarksBusy=marksBusy;Reason=reason; }
    }
    public interface IControlLease : IDisposable { InstanceRef Id { get; } InstanceRef Actor { get; } ScopeRef Owner { get; } ControlChannel Channels { get; } bool IsActive { get; } }
    public interface IActorControl
    {
        Result<IControlLease> TryAcquire(ControlRequest request);
        Result<Unit> Release(InstanceRef lease);
        void ReleaseOwner(ScopeRef owner);
        int ActiveLeaseCount { get; }
    }
    public interface IActorCommands
    {
        Result<Unit> Move(InstanceRef actor,Point2 normalizedDirection,InstanceRef? lease);
        Result<Unit> SetState(InstanceRef actor,string stateId);
        Result<Unit> ToggleAutoFire(InstanceRef actor,string inputId);
    }
    public sealed class DamageCandidate : IHookCandidate
    {
        public string HookId => "combat.before_damage_commit";
        public InstanceRef Target { get; } public InstanceRef? Source { get; } public double Amount { get; private set; }
        public DamageCandidate(InstanceRef target,InstanceRef? source,double amount) { Target=target;Source=source;Set(amount); }
        private void Set(double v) { if(double.IsNaN(v)||double.IsInfinity(v)||v<0)throw new ArgumentException("Invalid damage"); Amount=v; }
        public void Scale(double factor) { if(double.IsNaN(factor)||double.IsInfinity(factor)||factor<0)throw new ArgumentException("Invalid scale"); Set(Amount*factor); }
    }
    public interface ICombatCommands { Result<IActionInstance> ExecuteAttack(InstanceRef actor,ContentId attack,InstanceRef target,ScopeRef owner); Result<Unit> CancelOwnedAttacks(InstanceRef actor,string reason); }
    public interface IInteractionCommands { Result<string> Confirm(InstanceRef actor,InstanceRef target,string interactionId,string inputId); }
    public enum QuestState { Inactive, Active, Completed, Failed, Cancelled }
    public interface IQuestQuery { Result<QuestState> State(InstanceRef quest); Result<bool> ObjectiveSatisfied(InstanceRef quest,string objectiveId); }
    public interface IQuestCommands { Result<InstanceRef> Activate(ContentId quest,ScopeRef owner); }
    public interface IDialogueCommands { Result<IActionInstance> Start(ContentId dialogue,ScopeRef owner,IReadOnlyDictionary<string,InstanceRef> speakers,string openingInputId); Result<Unit> Submit(DialogueInput input); }
    public interface ILevelCommands { Result<Unit> RequestComplete(InstanceRef level); Result<Unit> End(InstanceRef level,string reason); }
}
