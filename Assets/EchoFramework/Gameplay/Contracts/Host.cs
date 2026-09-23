using System;
using System.Collections.Generic;
using Echo.Framework.Contracts;
namespace Echo.Gameplay.Contracts
{
    public enum InputKind { Move, ToggleAutoFire, Interact, DialogueAdvance, DialogueChoose, DialogueClose }
    public sealed class DialogueInput
    {
        public string InputId { get; } public InstanceRef Session { get; } public string NodeId { get; } public long Revision { get; } public string ChoiceId { get; } public InputKind Kind { get; }
        public DialogueInput(string inputId,InstanceRef session,string nodeId,long revision,string choiceId,InputKind kind) { InputId=inputId;Session=session;NodeId=nodeId;Revision=revision;ChoiceId=choiceId;Kind=kind; }
    }
    public sealed class InputIntent
    {
        public string InputId { get; } public string RunId { get; } public long Sequence { get; } public long Tick { get; } public InputKind Kind { get; }
        public InstanceRef? Actor { get; } public Point2 Direction { get; } public InstanceRef? Target { get; } public string InteractionId { get; } public DialogueInput Dialogue { get; }
        public InputIntent(string inputId,string runId,long sequence,long tick,InputKind kind,InstanceRef? actor,Point2 direction,InstanceRef? target,string interactionId,DialogueInput dialogue)
        { InputId=inputId;RunId=runId;Sequence=sequence;Tick=tick;Kind=kind;Actor=actor;Direction=direction;Target=target;InteractionId=interactionId;Dialogue=dialogue; }
    }
    public interface ILogicWorld : IDisposable
    {
        Result<Unit> Start(StartupRequest request);
        Result<Unit> Submit(InputIntent intent);
        Result<Unit> Step(); void SetSimulationPaused(bool paused);
        WorldSnapshot Snapshot { get; }
        Result<Unit> End(string reason);
    }
    public sealed class EntityPresentation
    {
        public EntityState Entity { get; } public Point2 Facing { get; } public string AnimationSemantic { get; } public string AttackPhase { get; }
        public double PhaseProgress { get; } public ContentId Presentation { get; } public bool Alive { get; } public bool AutoFire { get; }
        public EntityPresentation(EntityState entity,Point2 facing,string animationSemantic,string attackPhase,double phaseProgress,ContentId presentation,bool alive,bool autoFire)
        { Entity=entity;Facing=facing;AnimationSemantic=animationSemantic;AttackPhase=attackPhase;PhaseProgress=phaseProgress;Presentation=presentation;Alive=alive;AutoFire=autoFire; }
    }
    public sealed class DoorSnapshot { public InstanceRef Door { get; } public bool Open { get; } public DoorSnapshot(InstanceRef door,bool open) { Door=door;Open=open; } }
    public sealed class ObjectiveSnapshot { public InstanceRef Quest { get; } public string ObjectiveId { get; } public long Progress { get; } public long Required { get; } public QuestState State { get; } public ObjectiveSnapshot(InstanceRef quest,string id,long progress,long required,QuestState state) { Quest=quest;ObjectiveId=id;Progress=progress;Required=required;State=state; } }
    public sealed class InteractionPrompt { public InstanceRef Actor { get; } public InstanceRef Target { get; } public string InteractionId { get; } public string Text { get; } public bool Available { get; } public string Reason { get; } public InteractionPrompt(InstanceRef actor,InstanceRef target,string id,string text,bool available,string reason) { Actor=actor;Target=target;InteractionId=id;Text=text;Available=available;Reason=reason; } }
    public sealed class DialogueSnapshot
    {
        public InstanceRef Session { get; } public string NodeId { get; } public long Revision { get; } public string Speaker { get; } public string Text { get; } public IReadOnlyDictionary<string,string> Choices { get; }
        public DialogueSnapshot(InstanceRef session,string nodeId,long revision,string speaker,string text,IDictionary<string,string> choices) { Session=session;NodeId=nodeId;Revision=revision;Speaker=speaker;Text=text;Choices=Frozen.Map(choices); }
    }
    public sealed class LevelResult { public string ResultId { get; } public bool Success { get; } public string Reason { get; } public LevelResult(string id,bool success,string reason) { ResultId=id;Success=success;Reason=reason; } }
    public sealed class WorldSnapshot
    {
        public string RunId { get; } public long Tick { get; } public IReadOnlyList<EntityPresentation> Entities { get; } public IReadOnlyList<DoorSnapshot> Doors { get; }
        public IReadOnlyList<ObjectiveSnapshot> Objectives { get; } public IReadOnlyList<InteractionPrompt> Interactions { get; } public IReadOnlyList<DialogueSnapshot> Dialogues { get; } public LevelResult Result { get; }
        public WorldSnapshot(string runId,long tick,IEnumerable<EntityPresentation> entities,IEnumerable<DoorSnapshot> doors,IEnumerable<ObjectiveSnapshot> objectives,IEnumerable<InteractionPrompt> interactions,IEnumerable<DialogueSnapshot> dialogues,LevelResult result)
        { RunId=runId;Tick=tick;Entities=Frozen.List(entities);Doors=Frozen.List(doors);Objectives=Frozen.List(objectives);Interactions=Frozen.List(interactions);Dialogues=Frozen.List(dialogues);Result=result; }
    }
    public enum ResourceType { Sprite, Texture, Audio, Text, Font }
    public sealed class ResourceEntry
    {
        public ContentId Id { get; } public ResourceType Type { get; } public string SourcePath { get; } public string Sha256 { get; } public string License { get; }
        public ResourceEntry(ContentId id,ResourceType type,string sourcePath,string sha256,string license) { Id=id;Type=type;SourcePath=sourcePath;Sha256=sha256;License=license; }
    }
    public interface IResourceManifest { IReadOnlyList<ResourceEntry> Entries { get; } Result<ResourceEntry> Resolve(ContentId id,ResourceType expectedType); }
    // Unity objects stay in the host's generic adapter, never in gameplay state.
    public interface IResourceProvider<T> { Result<T> Load(ContentId id,ResourceType expectedType); }
}
