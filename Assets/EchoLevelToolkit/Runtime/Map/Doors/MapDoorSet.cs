using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Map.Doors
{
    // Sole owner of effective door state. Story requests never bypass an encounter lock.
    public sealed class MapDoorSet : MonoBehaviour
    {
        private sealed class Entry
        {
            public MapDoor Door;
            public ContentIdentity LockOwner;
            public bool HasOwner;
            public bool Locked;
            public bool RequestedOpen;
        }

        [SerializeField] private MapDoor[] doors = Array.Empty<MapDoor>();
        private readonly Dictionary<ContentIdentity, Entry> entries = new Dictionary<ContentIdentity, Entry>();
        private readonly List<IDisposable> registrations = new List<IDisposable>();
        private RuntimeScope scope;
        public RuntimeScope Scope => scope;
        public IReadOnlyList<MapDoor> ConfiguredDoors => doors ?? Array.Empty<MapDoor>();
        public bool IsInitialized { get; private set; }

        public bool Initialize(RuntimeScope ownerScope)
        {
            if (IsInitialized || !ownerScope.RunId.IsValid || !ownerScope.InstanceId.IsValid) return false;
            scope = ownerScope;
            foreach (MapDoor door in ConfiguredDoors)
            {
                if (!door || !door.DoorId.IsComplete || !door.Blocker || entries.ContainsKey(door.DoorId))
                { ResetState(); return false; }
                var entry = new Entry { Door = door, RequestedOpen = door.InitiallyOpen };
                entries.Add(door.DoorId, entry);
                door.ApplyOpen(entry.RequestedOpen);
            }
            IsInitialized = true;
            return true;
        }

        public bool Reserve(ContentIdentity encounterId, IReadOnlyList<ContentIdentity> doorIds)
        {
            if (!IsInitialized || !encounterId.IsComplete || doorIds == null) return false;
            var unique = new HashSet<ContentIdentity>();
            foreach (ContentIdentity id in doorIds)
                if (!unique.Add(id) || !entries.TryGetValue(id, out var entry)
                    || entry.HasOwner && !entry.LockOwner.Equals(encounterId)) return false;
            foreach (ContentIdentity id in unique)
            {
                Entry entry = entries[id];
                entry.HasOwner = true;
                entry.LockOwner = encounterId;
            }
            return true;
        }

        public bool CanLock(ContentIdentity encounterId, IReadOnlyList<ContentIdentity> doorIds,
            Collider2D playerCollider)
        {
            if (!IsInitialized || !playerCollider || doorIds == null) return false;
            foreach (ContentIdentity id in doorIds)
            {
                if (!entries.TryGetValue(id, out var entry) || !entry.HasOwner
                    || !entry.LockOwner.Equals(encounterId)) return false;
                if (!entry.Locked && entry.Door.ClosureBounds.Intersects(playerCollider.bounds)) return false;
            }
            return true;
        }

        public bool TryLock(ContentIdentity encounterId, IReadOnlyList<ContentIdentity> doorIds,
            Collider2D playerCollider)
        {
            if (!CanLock(encounterId, doorIds, playerCollider)) return false;
            foreach (ContentIdentity id in doorIds)
            {
                Entry entry = entries[id];
                entry.Locked = true;
                entry.Door.ApplyOpen(false);
            }
            return true;
        }

        public void Unlock(ContentIdentity encounterId, IReadOnlyList<ContentIdentity> doorIds)
        {
            if (!IsInitialized || doorIds == null) return;
            foreach (ContentIdentity id in doorIds)
            {
                if (!entries.TryGetValue(id, out var entry) || !entry.HasOwner
                    || !entry.LockOwner.Equals(encounterId)) continue;
                entry.Locked = false;
                entry.RequestedOpen = true;
                entry.Door.ApplyOpen(entry.RequestedOpen);
            }
        }

        public LevelActionResult RequestOpen(ContentIdentity doorId, bool open)
        {
            if (!IsInitialized || !entries.TryGetValue(doorId, out var entry))
                return LevelActionResult.TargetMissing;
            if (entry.RequestedOpen == open)
                return entry.Locked && open ? LevelActionResult.ConditionNotMet : LevelActionResult.AlreadySatisfied;
            entry.RequestedOpen = open;
            entry.Door.ApplyOpen(open && !entry.Locked);
            return entry.Locked && open ? LevelActionResult.ConditionNotMet : LevelActionResult.Success;
        }

        public bool IsOpen(ContentIdentity doorId)
        {
            return IsInitialized && entries.TryGetValue(doorId, out var entry)
                && entry.RequestedOpen && !entry.Locked;
        }

        public bool RegisterActions(LevelBindingSession session)
        {
            if (!IsInitialized || session == null || session.Context.Scope != scope) return false;
            try
            {
                foreach (MapDoor door in ConfiguredDoors)
                {
                    if (!door.SetOpenEndpoint.IsComplete) continue;
                    ContentIdentity doorId = door.DoorId;
                    registrations.Add(session.RegisterActionTarget(door.SetOpenEndpoint,
                        new DoorAction(this, doorId)));
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                foreach (IDisposable registration in registrations) registration.Dispose();
                registrations.Clear();
                return false;
            }
        }

        public void ResetState()
        {
            foreach (IDisposable registration in registrations) registration.Dispose();
            registrations.Clear();
            foreach (Entry entry in entries.Values) if (entry.Door) entry.Door.ApplyOpen(entry.Door.InitiallyOpen);
            entries.Clear();
            scope = default;
            IsInitialized = false;
        }

        private void OnDestroy() { ResetState(); }

        private sealed class DoorAction : ILevelActionTarget
        {
            private readonly MapDoorSet owner;
            private readonly ContentIdentity doorId;
            public DoorAction(MapDoorSet owner, ContentIdentity doorId)
            { this.owner = owner; this.doorId = doorId; }
            public LevelActionResult Execute(LevelActionCommand command)
            {
                if (command.Kind != LevelEndpointKind.SetDoorOpen || command.Scope != owner.scope)
                    return LevelActionResult.InvalidScope;
                return owner.RequestOpen(doorId, command.DesiredState);
            }
        }
    }
}
