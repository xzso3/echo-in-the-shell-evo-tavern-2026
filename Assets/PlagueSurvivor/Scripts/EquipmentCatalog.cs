using System;
using UnityEngine;

namespace PlagueSurvivor
{
    [CreateAssetMenu(menuName = "Neural Lockdown/Equipment Catalog")]
    public sealed class EquipmentCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class QualityTier
        {
            public EquipmentQuality quality;
            public Color color = Color.white;
            [Min(0)] public float weight = 1;
            [Min(0)] public float movePercent;
            [Min(0)] public float attackPercent;
            [Min(0)] public float attackPower;
            public QualityTier(EquipmentQuality q, Color c, float w, float move, float attack, float damage)
            { quality = q; color = c; weight = w; movePercent = move; attackPercent = attack; attackPower = damage; }
        }
        public QualityTier[] tiers = {
            new QualityTier(EquipmentQuality.Poor, new Color(.52f,.56f,.60f), 35, 5, 8, 4),
            new QualityTier(EquipmentQuality.Common, new Color(.86f,.91f,.94f), 40, 10, 15, 10),
            new QualityTier(EquipmentQuality.Rare, new Color(.08f,.78f,1), 18, 18, 25, 20),
            new QualityTier(EquipmentQuality.Epic, new Color(.72f,.32f,1), 6, 28, 40, 36),
            new QualityTier(EquipmentQuality.Legendary, new Color(1,.64f,.10f), 1, 40, 60, 60)
        };
        [Tooltip("Feet, Module, Weapon; five 256px quality cells per atlas.")]
        public Texture2D[] atlases = new Texture2D[3];
        public Material worldMaterial;
        [Range(0, 1)] public float dropChance = .30f;
        [Min(.1f)] public float magnetRadius = 2.8f;
        [Min(.1f)] public float pickupRadius = .45f;
        [Min(.1f)] public float magnetSpeed = 8;
        [NonSerialized] Sprite[] icons;
        public QualityTier Tier(EquipmentQuality quality)
        {
            foreach (var tier in tiers) if (tier.quality == quality) return tier;
            throw new InvalidOperationException("Missing equipment quality: " + quality);
        }
        public EquipmentItem Create(EquipmentSlot slot, EquipmentQuality quality)
        {
            var tier = Tier(quality);
            return new EquipmentItem(slot, quality, slot == EquipmentSlot.Weapon ? tier.attackPower : slot == EquipmentSlot.Feet ? tier.movePercent : tier.attackPercent);
        }
        public EquipmentItem Roll()
        {
            float total = 0;
            foreach (var tier in tiers) total += Mathf.Max(0, tier.weight);
            float value = UnityEngine.Random.value * total;
            var quality = EquipmentQuality.Poor;
            foreach (var tier in tiers)
            {
                value -= Mathf.Max(0, tier.weight);
                if (value <= 0) { quality = tier.quality; break; }
            }
            return Create((EquipmentSlot)UnityEngine.Random.Range(0, 3), quality);
        }
        public Sprite Icon(EquipmentItem item)
        {
            if (item == null) return null;
            if (icons == null) icons = new Sprite[15];
            int index = (int)item.Slot * 5 + (int)item.Quality;
            if (!icons[index])
            {
                var texture = atlases[(int)item.Slot];
                if (!texture) return null;
                // Runtime atlas views, without altering source sprite metadata.
                icons[index] = Sprite.Create(texture, new Rect((int)item.Quality * 256, 0, 256, 256), new Vector2(.5f,.5f), 256);
                icons[index].name = item.Slot + "_" + item.Quality;
            }
            return icons[index];
        }
        void OnDisable()
        {
            if (icons == null) return;
            foreach (var sprite in icons) if (sprite) { if (Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); }
            icons = null;
        }
    }
}
