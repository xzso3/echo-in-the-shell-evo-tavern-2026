using System;
using UnityEngine;

namespace PlagueSurvivor
{
    public enum EquipmentSlot { Feet, Module, Weapon }
    public enum EquipmentQuality { Poor, Common, Rare, Epic, Legendary }

    // Each drop is a distinct item, even when type and quality match.
    public sealed class EquipmentItem
    {
        public readonly EquipmentSlot Slot;
        public readonly EquipmentQuality Quality;
        public readonly float Value;
        public EquipmentItem(EquipmentSlot slot, EquipmentQuality quality, float value)
        { Slot = slot; Quality = quality; Value = Mathf.Max(0, value); }
        public string Name { get { return Slot == EquipmentSlot.Weapon ? "Smart Weapon" : Slot == EquipmentSlot.Feet ? "Mobility Boots" : "Reflex Module"; } }
        public string StatName { get { return Slot == EquipmentSlot.Weapon ? "Attack power" : Slot == EquipmentSlot.Feet ? "Move speed" : "Attack speed"; } }
        public string Suffix { get { return Slot == EquipmentSlot.Weapon ? "" : "%"; } }
        public string Affix { get { return StatName + " +" + Value.ToString("0.#") + Suffix; } }
    }

    [Serializable]
    public sealed class PlayerEquipment
    {
        public const int Capacity = 24;
        [NonSerialized] EquipmentItem[] bag = new EquipmentItem[Capacity];
        [NonSerialized] EquipmentItem[] equipped = new EquipmentItem[3];
        public event Action Changed;
        public int Count { get { int n = 0; foreach (var item in bag) if (item != null) n++; return n; } }
        public bool Full { get { return Count == Capacity; } }
        public EquipmentItem At(int index) { return index >= 0 && index < Capacity ? bag[index] : null; }
        public EquipmentItem Equipped(EquipmentSlot slot) { int i = (int)slot; return i >= 0 && i < equipped.Length ? equipped[i] : null; }
        public bool TryAdd(EquipmentItem item)
        {
            if (item == null || (int)item.Slot < 0 || (int)item.Slot >= equipped.Length) return false;
            foreach (var held in bag) if (ReferenceEquals(held, item)) return false;
            foreach (var held in equipped) if (ReferenceEquals(held, item)) return false;
            int empty = Array.FindIndex(bag, x => x == null);
            if (empty < 0) return false;
            bag[empty] = item; Notify(); return true;
        }
        public bool Equip(int index)
        {
            var item = At(index);
            if (item == null) return false;
            // Replacing gear uses the source cell, including when the bag is full.
            int slot = (int)item.Slot;
            bag[index] = equipped[slot]; equipped[slot] = item;
            Notify(); return true;
        }
        public bool Unequip(EquipmentSlot slot)
        {
            if (Equipped(slot) == null) return false;
            int empty = Array.FindIndex(bag, x => x == null);
            if (empty < 0) return false;
            bag[empty] = equipped[(int)slot]; equipped[(int)slot] = null;
            Notify(); return true;
        }
        public bool Delete(int index)
        {
            if (At(index) == null) return false;
            bag[index] = null; Notify(); return true;
        }
        public void Reset() { bag = new EquipmentItem[Capacity]; equipped = new EquipmentItem[3]; Notify(); }
        void Notify() { if (Changed != null) Changed(); }
        public float MoveBonus { get { var item = Equipped(EquipmentSlot.Feet); return item == null ? 0 : item.Value; } }
        public float AttackBonus { get { var item = Equipped(EquipmentSlot.Module); return item == null ? 0 : item.Value; } }
        public float DamageBonus { get { var item = Equipped(EquipmentSlot.Weapon); return item == null ? 0 : item.Value; } }
        public float MoveSpeed(float baseSpeed) { return Mathf.Max(0, baseSpeed) * (1 + MoveBonus / 100); }
        public float AttackInterval(float baseInterval) { return Mathf.Max(.01f, baseInterval / (1 + AttackBonus / 100)); }
        public float Damage(float baseDamage) { return Mathf.Max(0, baseDamage) + DamageBonus; }
    }
}
