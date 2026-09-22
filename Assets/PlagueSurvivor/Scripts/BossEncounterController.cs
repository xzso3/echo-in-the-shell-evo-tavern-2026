using UnityEngine;
using TMPro;

namespace PlagueSurvivor
{
    public enum BossEncounterState { Preparation, Startup, PhaseOne, Transition, PhaseTwo, Victory, Defeat }
    public sealed class BossEncounterController : MonoBehaviour
    {
        public BossArenaConfig config;
        public SpriteRenderer startMarker;
        public TMP_Text bossLabel, skillLabel;
        public UnityEngine.UI.Image bossHealthFill;
        public BossEncounterState State { get; private set; }
        public float Health { get; private set; }
        public bool PhaseTwo { get; private set; }
        public float StateClock { get; private set; }
        public WardenBossController Warden { get; private set; }
        public BossSupplyController Supplies { get; private set; }
        public Vector2 BossPosition { get { return Vector2.zero; } }
        public bool CombatActive { get { return State == BossEncounterState.PhaseOne || State == BossEncounterState.PhaseTwo; } }
        public bool Finished { get { return State == BossEncounterState.Victory || State == BossEncounterState.Defeat; } }
        PlagueGame game;
        public void Initialize(PlagueGame owner)
        {
            game = owner;
            Warden = gameObject.AddComponent<WardenBossController>(); Warden.Initialize(this, game);
            Supplies = gameObject.AddComponent<BossSupplyController>(); Supplies.Initialize(this, game);
        }
        public void ResetEncounter()
        {
            State = BossEncounterState.Preparation; StateClock = 0; PhaseTwo = false; Health = config.health;
            Warden.ResetBoss(); Supplies.ResetSupplies(); startMarker.gameObject.SetActive(true); UpdateHUD();
        }
        public Vector2 MovePlayer(Vector2 from, Vector2 delta)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .15f)); delta /= steps;
            for (int i = 0; i < steps; i++)
            {
                Vector2 p = from + delta;
                float r = config.bodyRadius + config.playerRadius;
                if (p.magnitude < r) p = p.sqrMagnitude < .0001f ? Vector2.down * r : p.normalized * r;
                from = new Vector2(Mathf.Clamp(p.x, -config.halfSize.x + config.playerRadius, config.halfSize.x - config.playerRadius),
                    Mathf.Clamp(p.y, -config.halfSize.y + config.playerRadius, config.halfSize.y - config.playerRadius));
            }
            return from;
        }
        public void Tick(float dt)
        {
            if (game.Paused || Finished) return;
            StateClock += dt;
            if (State == BossEncounterState.Preparation && Vector2.Distance(game.PlayerPosition, config.startPosition) <= config.startRadius)
            { State = BossEncounterState.Startup; StateClock = 0; startMarker.gameObject.SetActive(false); }
            else if (State == BossEncounterState.Startup && StateClock >= config.startupSeconds)
            { State = BossEncounterState.PhaseOne; StateClock = 0; Warden.BeginPhase(); }
            else if (State == BossEncounterState.Transition && StateClock >= config.transitionSeconds && !Supplies.DeliveryPending && Supplies.PendingCount == 0)
            { State = BossEncounterState.PhaseTwo; StateClock = 0; Warden.BeginPhase(); }
            else if (CombatActive) Warden.Tick(dt);
            if (game.IsDead) { Finish(false); return; }
            Supplies.Tick(dt, State == BossEncounterState.Transition || (CombatActive && Warden.Recovering));
        }
        public void ReceiveDamage(float amount)
        {
            if (!CombatActive || game.Paused || game.IsDead || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            if (Health > 0) Supplies.ObserveHealth(Health / config.health);
        }
        // Called after projectile iteration, so clearing shots cannot invalidate the damage loop.
        public void ResolveState()
        {
            if (Finished) return;
            if (game.IsDead) { Finish(false); return; }
            if (Health <= 0) { Finish(true); return; }
            if (State == BossEncounterState.PhaseOne && Health <= config.health * .5f)
            {
                PhaseTwo = true; State = BossEncounterState.Transition; StateClock = 0;
                Warden.ClearHazards(); game.ClearProjectiles();
                Warden.ShowOverload();
            }
        }
        void Finish(bool won)
        {
            State = won ? BossEncounterState.Victory : BossEncounterState.Defeat;
            Warden.ClearHazards(); Supplies.Clear(); game.ClearProjectiles(); game.Loot.Clear();
            if (won) Warden.SetWreck();
            game.ShowEncounterResult((won ? "WARDEN-01 DISABLED" : "CONNECTION LOST") +
                "\n<size=24>" + (won ? "DISTRICT SECURED" : "RECALIBRATE. TRY AGAIN.") + "\n" + game.Elapsed.ToString("0.0") +
                "s   /   " + game.DamageEvents + " hits received\n<size=18>Burst " + Warden.BurstHits + "   Bomb " + Warden.BombHits +
                "   Grid " + Warden.GridHits + "   Medical " + Supplies.MedicalPicked + "</size>\n\nPress R to restart</size>");
            UpdateHUD();
        }
        public void UpdateHUD()
        {
            if (!game || !Warden) return;
            bossLabel.text = "WARDEN-01    /    " + (PhaseTwo ? "OVERLOAD" : "DISTRICT WARDEN") + "    " + Mathf.CeilToInt(Health) + " / " + config.health;
            bossHealthFill.fillAmount = Health / config.health;
            bossHealthFill.color = PhaseTwo ? new Color(1, .34f, .12f) : new Color(.1f, .9f, 1);
            string cue = Warden.Cue;
            if (State == BossEncounterState.Preparation) cue = "COLLECT GEAR  /  E TO EQUIP  /  ENTER THE CYAN START PAD";
            else if (State == BossEncounterState.Startup) cue = "WARDEN ACTIVATING  /  " + Mathf.CeilToInt(config.startupSeconds - StateClock);
            else if (State == BossEncounterState.Transition) cue = "CORE OVERLOAD  /  SECTOR CLEAR  /  SUPPLY INCOMING";
            else if (Finished) cue = State == BossEncounterState.Victory ? "SECTOR SECURED" : "SIGNAL LOST";
            if (CombatActive && Vector2.Distance(game.PlayerPosition, BossPosition) > game.attackRange) cue += "   [OUT OF RANGE]";
            skillLabel.text = cue;
            game.status.text = "INTEGRITY  " + Mathf.CeilToInt(game.Health) + " / " + game.maxHealth;
            game.message.text = game.Loot.NearbyBagFull ? "BACKPACK FULL  /  E TO MANAGE  /  EQUIPMENT STAYS ON THE GROUND"
                : "WASD Move    AUTO FIRE    E Bag " + game.equipment.Count + "/24    ESC Pause    R Restart";
        }
    }
}
