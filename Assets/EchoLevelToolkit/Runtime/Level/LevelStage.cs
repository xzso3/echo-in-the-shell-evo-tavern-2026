using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Map.Doors;
using UnityEngine;

namespace Echo.LevelToolkit.Level
{
    // One placed level instance. A restart creates a fresh scene/prefab, RunId and session.
    public sealed class LevelStage : MonoBehaviour
    {
        [SerializeField] private ContentIdentity levelId;
        [SerializeField] private MapDoorSet doorSet;
        [SerializeField] private LevelEncounter[] encounters = Array.Empty<LevelEncounter>();
        [SerializeField] private LevelExit[] exits = Array.Empty<LevelExit>();
        [SerializeField] private Transform playerSpawn;
        private LevelBindingSession session;
        public ContentIdentity LevelId => levelId;
        public MapDoorSet Doors => doorSet;
        public IReadOnlyList<LevelEncounter> Encounters => encounters ?? Array.Empty<LevelEncounter>();
        public IReadOnlyList<LevelExit> Exits => exits ?? Array.Empty<LevelExit>();
        public Transform PlayerSpawn => playerSpawn;

        public bool Prepare(CombatWorld world, LevelBindingSession binding)
        {
            if (session != null || !world || binding == null || !levelId.IsComplete || !doorSet
                || !levelId.Equals(binding.Context.Scope.Content)
                || world.Context.Scope != binding.Context.Scope || !doorSet.Initialize(binding.Context.Scope))
                return false;
            var usedEnemies = new HashSet<CombatEnemy>();
            var usedBosses = new HashSet<CombatBoss>();
            var ids = new HashSet<ContentIdentity>();
            foreach (LevelEncounter encounter in Encounters)
            {
                if (!encounter || !ids.Add(encounter.EncounterId)) { Dispose(); return false; }
                foreach (CombatEnemy enemy in encounter.Enemies)
                    if (!enemy || !usedEnemies.Add(enemy)) { Dispose(); return false; }
                if (encounter.Boss && !usedBosses.Add(encounter.Boss)) { Dispose(); return false; }
            }
            foreach (LevelExit exit in Exits)
                if (!exit || !ids.Add(exit.ExitId)) { Dispose(); return false; }
            session = binding;
            foreach (LevelEncounter encounter in Encounters)
                if (!encounter.Prepare(world, doorSet, binding)) { Dispose(); return false; }
            foreach (LevelExit exit in Exits)
                if (!exit.Prepare(world, binding)) { Dispose(); return false; }
            if (!doorSet.RegisterActions(binding)) { Dispose(); return false; }
            return true;
        }

        public bool StartBound()
        {
            if (session == null || !session.IsActive) return false;
            foreach (LevelEncounter encounter in Encounters)
                if (!encounter.OnBindingStarted()) return false;
            return true;
        }

        public void Dispose()
        {
            foreach (LevelEncounter encounter in Encounters) if (encounter) encounter.Dispose();
            foreach (LevelExit exit in Exits) if (exit) exit.Dispose();
            if (doorSet) doorSet.ResetState();
            session = null;
        }
        private void OnDestroy() { Dispose(); }
    }
}
