using System.Collections.Generic;
using UnityEngine;

namespace PlagueSurvivor
{
    public sealed class BossSupplyController : MonoBehaviour
    {
        sealed class Medical { public Vector2 position; public SpriteRenderer view; }
        readonly Queue<int> pending = new Queue<int>();
        readonly List<Medical> medicals = new List<Medical>();
        readonly bool[] triggered = new bool[3];
        BossEncounterController encounter;
        PlagueGame game;
        BossArenaConfig config;
        Transform root, marker;
        Vector2 landing;
        float clock;
        int delivering, nextPoint;
        public int Delivered { get; private set; }
        public int MedicalPicked { get; private set; }
        public int MedicalCount { get { return medicals.Count; } }
        public int PendingCount { get { return pending.Count + (marker ? 1 : 0); } }
        public bool DeliveryPending { get { return marker != null; } }
        public float DeliveryClock { get { return clock; } }
        public void Initialize(BossEncounterController owner, PlagueGame playerGame)
        { encounter = owner; game = playerGame; config = owner.config; root = new GameObject("Boss Supplies").transform; root.SetParent(transform, false); }
        public void ResetSupplies()
        {
            Clear(); for (int i = 0; i < triggered.Length; i++) triggered[i] = false;
            Delivered = MedicalPicked = nextPoint = 0;
            var c = game.equipmentCatalog;
            game.Loot.Spawn(c.Create(EquipmentSlot.Weapon, EquipmentQuality.Common), config.playerSpawn + new Vector2(-1.9f, .4f));
            game.Loot.Spawn(c.Create(EquipmentSlot.Feet, EquipmentQuality.Common), config.playerSpawn + new Vector2(1.9f, .4f));
            game.Loot.Spawn(c.Create(EquipmentSlot.Module, EquipmentQuality.Common), config.playerSpawn + new Vector2(0, -.6f));
        }
        public void ObserveHealth(float ratio)
        {
            for (int i = 0; i < 3; i++)
                if (!triggered[i] && ratio <= .75f - i * .25f) { triggered[i] = true; pending.Enqueue(i); }
        }
        bool FindPoint(out Vector2 point)
        {
            for (int i = 0; i < config.supplyPoints.Length; i++)
            {
                int index = (nextPoint + i) % config.supplyPoints.Length;
                point = config.supplyPoints[index];
                if (Mathf.Abs(point.x) > config.halfSize.x - 1.5f || Mathf.Abs(point.y) > config.halfSize.y - 1.5f || point.magnitude < config.bodyRadius + 1) continue;
                if (game.Loot.Occupied(point, 1.2f)) continue;
                bool occupied = false;
                foreach (var med in medicals) if (Vector2.Distance(point, med.position) < 1.2f) occupied = true;
                if (!occupied) { nextPoint = (index + 1) % config.supplyPoints.Length; return true; }
            }
            point = Vector2.zero; return false;
        }
        public void Tick(float dt, bool safeWindow)
        {
            if (!marker && safeWindow && pending.Count > 0 && FindPoint(out landing))
            {
                delivering = pending.Dequeue(); clock = 0;
                marker = BossArenaVisual.Sprite("Incoming supply - square", config.supplyMarker, landing, root, config, 70).transform;
            }
            if (marker)
            {
                clock += dt;
                marker.localScale = Vector3.one * (1 + .08f * Mathf.Sin(clock * 8));
                if (clock >= config.supplyWarning)
                {
                    BossArenaVisual.Remove(marker); marker = null; Delivered++;
                    if (delivering == 1) game.Loot.Spawn(game.equipmentCatalog.Create(EquipmentSlot.Weapon, EquipmentQuality.Rare), landing);
                    else medicals.Add(new Medical { position = landing, view = BossArenaVisual.Sprite("Medical +25 - full health preserves", config.medkit, landing, root, config, 180) });
                }
            }
            for (int i = medicals.Count - 1; i >= 0; i--)
            {
                var med = medicals[i];
                if (Vector2.Distance(game.PlayerPosition, med.position) <= config.medicalRadius && game.Heal(config.medicalHeal))
                { MedicalPicked++; BossArenaVisual.Remove(med.view.transform); medicals.RemoveAt(i); }
            }
        }
        public void Clear()
        {
            pending.Clear(); medicals.Clear(); marker = null; clock = 0;
            if (root) for (int i = root.childCount - 1; i >= 0; i--) BossArenaVisual.Remove(root.GetChild(i));
        }
    }
}
