using System.Collections.Generic;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.NativeGame.ToolkitIntegration.LevelHost;
using UnityEngine;

namespace Echo.NativeGame.GameFlow.Results
{
    // Retains actual weapon counts when a mounted Toolkit instance leaves the run.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NativeRunController))]
    public sealed class RunKillLedger : MonoBehaviour
    {
        [SerializeField] NativeLevelHost levelHost;
        readonly Dictionary<RuntimeScope, CombatWeapon> mountedWeapons =
            new Dictionary<RuntimeScope, CombatWeapon>();
        NativeLevelHost host;
        int unmountedKills;

        void OnEnable() { BindHost(); }
        void OnDisable() { UnbindHost(); }

        // INT may supply a host kept on another GameObject during run preparation.
        public void SetHost(NativeLevelHost selected)
        {
            levelHost = selected;
            BindHost();
        }

        public int ReadKills(NativeRunController run)
        {
            BindHost();
            int kills = run && run.combat ? run.combat.Kills : 0;
            kills += unmountedKills;
            foreach (CombatWeapon weapon in mountedWeapons.Values)
                if (weapon) kills += weapon.Kills;
            return kills;
        }

        void BindHost()
        {
            var found = levelHost ? levelHost : GetComponent<NativeLevelHost>();
            if (host != found)
            {
                UnbindHost();
                host = found;
                if (host)
                {
                    host.Mounted += OnMounted;
                    host.Unmounted += OnUnmounted;
                }
            }
            if (host)
                foreach (NativeLevelInstance instance in host.Instances) OnMounted(instance);
        }

        void UnbindHost()
        {
            if (!host) return;
            host.Mounted -= OnMounted;
            host.Unmounted -= OnUnmounted;
            host = null;
        }

        void OnMounted(NativeLevelInstance instance)
        {
            if (instance != null && instance.Placement && instance.Placement.Weapon)
                mountedWeapons[instance.Scope] = instance.Placement.Weapon;
        }

        void OnUnmounted(NativeLevelInstance instance)
        {
            if (instance == null || !mountedWeapons.TryGetValue(instance.Scope, out CombatWeapon weapon))
                return;
            // Unmounted is emitted after Dispose schedules the placement for Destroy.
            // Kills is a managed counter, so read the retained component reference now.
            unmountedKills += weapon.Kills;
            mountedWeapons.Remove(instance.Scope);
        }
    }
}
