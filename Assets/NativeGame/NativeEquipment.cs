using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Echo.NativeGame
{
    // Owns the one piece of gear and its timed overclock. Combat fields are the live output.
    public sealed class NativeEquipment : MonoBehaviour
    {
        public NativeRunController run;
        public float damageBonus = 12f;
        public float overclockDuration = 8f;
        [Range(.1f, 1f)] public float overclockShotIntervalFactor = .6f;

        public bool HasCoil { get; private set; }
        public bool Equipped { get; private set; }
        public bool OverclockActive { get; private set; }
        public float OverclockSecondsLeft => OverclockActive ? Mathf.Max(0, overclockEndsAt - Time.time) : 0;
        public event Action Changed;

        float damageBeforeEquip;
        float intervalBeforeOverclock;
        float overclockEndsAt;

        void Update()
        {
            if (OverclockActive && (!run || !run.Running || run.IsCombatAdvancing && Time.time >= overclockEndsAt)) StopOverclock();
            if (!run || !run.IsCombatAdvancing || TextInputFocused() || run.dialogue && run.dialogue.IsOpen ||
                run.hud && run.hud.phonePanel && run.hud.phonePanel.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.F)) ToggleEquip();
            if (Input.GetKeyDown(KeyCode.Q)) TryStartOverclock();
        }

        public bool TryPickUp()
        {
            if (HasCoil || !run || !run.IsCombatAdvancing || !run.combat) return false;
            HasCoil = true;
            Changed?.Invoke();
            return true;
        }

        public bool ToggleEquip()
        {
            if (!HasCoil || !run || !run.IsCombatAdvancing || !run.combat) return false;
            if (Equipped)
            {
                StopOverclock();
                run.combat.damage = damageBeforeEquip;
                Equipped = false;
            }
            else
            {
                damageBeforeEquip = run.combat.damage;
                run.combat.damage = damageBeforeEquip + Mathf.Max(0, damageBonus);
                Equipped = true;
            }
            Changed?.Invoke();
            return true;
        }

        public bool TryStartOverclock()
        {
            if (!Equipped || OverclockActive || !run || !run.IsCombatAdvancing || !run.combat) return false;
            intervalBeforeOverclock = run.combat.shotInterval;
            run.combat.shotInterval = Mathf.Max(.01f, intervalBeforeOverclock * Mathf.Clamp(overclockShotIntervalFactor, .1f, 1f));
            overclockEndsAt = Time.time + Mathf.Max(.1f, overclockDuration);
            OverclockActive = true;
            Changed?.Invoke();
            return true;
        }

        void StopOverclock()
        {
            if (!OverclockActive) return;
            if (run && run.combat) run.combat.shotInterval = intervalBeforeOverclock;
            OverclockActive = false;
            overclockEndsAt = 0;
            Changed?.Invoke();
        }

        void OnDisable()
        {
            StopOverclock();
            if (!Equipped) return;
            if (run && run.combat) run.combat.damage = damageBeforeEquip;
            Equipped = false;
            Changed?.Invoke();
        }

        static bool TextInputFocused()
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            return selected && (selected.GetComponent<TMP_InputField>() || selected.GetComponent<UnityEngine.UI.InputField>());
        }
    }
}
