using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlagueSurvivor.Editor
{
    // Run explicitly through Unity MCP in Play Mode. Never grants items during normal play.
    public static class InventoryVerification
    {
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Check(bool condition, string message, List<string> results)
        { if (!condition) throw new Exception("Inventory check failed: " + message); results.Add(message); }
        static bool Near(float a, float b) { return Mathf.Abs(a-b) < .001f; }
        public static string[] Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var g = UnityEngine.Object.FindObjectOfType<PlagueGame>();
            var catalog = g.equipmentCatalog;
            var result = new List<string>();
            var randomState = UnityEngine.Random.state;
            var city = g.city; int limit = g.enemyLimit;
            float dropChance = catalog.dropChance, magnetRadius = catalog.magnetRadius;
            bool enabled = g.enabled;
            try
            {
                g.enabled = false;
                UnityEngine.Random.InitState(8412);
                var inventory = new PlayerEquipment();
                Check(inventory.Count == 0 && PlayerEquipment.Capacity == 24, "Empty 24-cell inventory", result);
                for (int type = 0; type < 3; type++)
                {
                    float previous = -1;
                    for (int quality = 0; quality < 5; quality++)
                    {
                        var item = catalog.Create((EquipmentSlot)type,(EquipmentQuality)quality);
                        var icon = catalog.Icon(item);
                        Check(item.Value > previous && icon && icon.rect.width == 256 && icon.rect.x == quality*256,
                            "Quality value and icon: " + item.Slot + " " + item.Quality, result);
                        previous = item.Value;
                    }
                }
                var first = catalog.Create(EquipmentSlot.Feet,EquipmentQuality.Common);
                Check(inventory.TryAdd(first) && !inventory.TryAdd(first), "Reject duplicate item instance", result);
                for (int i=1;i<24;i++) inventory.TryAdd(catalog.Create(EquipmentSlot.Feet,EquipmentQuality.Rare));
                Check(inventory.Full && !inventory.TryAdd(catalog.Roll()), "Full bag rejects 25th item", result);
                Check(inventory.Equip(0) && inventory.Count == 23 && ReferenceEquals(first,inventory.Equipped(EquipmentSlot.Feet)), "Equip frees source cell", result);
                inventory.TryAdd(catalog.Create(EquipmentSlot.Weapon,EquipmentQuality.Poor));
                var replacement = inventory.At(1);
                Check(inventory.Equip(1) && inventory.Count == 24 && ReferenceEquals(first,inventory.At(1)) && ReferenceEquals(replacement,inventory.Equipped(EquipmentSlot.Feet)), "Full bag swaps without losing old gear", result);
                Check(!inventory.Unequip(EquipmentSlot.Feet) && inventory.Full, "Full bag blocks unequip without loss", result);
                Check(inventory.Delete(3) && inventory.Unequip(EquipmentSlot.Feet) && inventory.Full, "Delete frees space for unequip", result);
                Check(!inventory.Delete(-1) && !inventory.Equip(24), "Invalid cell operations rejected", result);
                inventory.Reset();
                Check(inventory.Count == 0 && Near(inventory.Damage(22),22) && Near(inventory.MoveSpeed(4.5f),4.5f), "Reset removes all bonuses", result);
                var qualityCounts = new int[5]; var typeCounts = new int[3];
                for(int i=0;i<10000;i++) { var item=catalog.Roll(); qualityCounts[(int)item.Quality]++; typeCounts[(int)item.Slot]++; }
                Check(qualityCounts[0]>3000 && qualityCounts[1]>3500 && qualityCounts[2]>1400 && qualityCounts[3]>400 && qualityCounts[4]>50,
                    "Weighted loot covers all qualities: " + string.Join(",",qualityCounts),result);
                Check(typeCounts[0]>2800 && typeCounts[1]>2800 && typeCounts[2]>2800,"All three equipment types drop",result);

                g.city = null; g.enemyLimit = 0; g.Restart();
                Check(g.equipment.Count == 0 && g.Loot.Count == 0, "Game starts without test equipment",result);
                g.SetEquipmentOpen(true);
                var root = GameObject.Find("HUD/Inventory");
                var content = root.transform.Find("Content");
                var left = content.Find("Equipment40").GetComponent<RectTransform>();
                var right = content.Find("Backpack60").GetComponent<RectTransform>();
                Check(Near(left.anchorMax.x,.4f) && Near(right.anchorMin.x,.4f),"40/60 inventory columns",result);
                var grid = content.Find("Backpack60/Inner/Grid24");
                Check(grid.childCount == 24,"Exactly 24 visible inventory cells",result);
                g.equipment.TryAdd(catalog.Create(EquipmentSlot.Weapon,EquipmentQuality.Legendary));
                grid.GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                content.Find("Backpack60/Inner/EquipAction").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Check(Near(g.EffectiveDamage,82) && g.equipment.Count==0,"UI equips weapon and adds attack power",result);
                g.equipment.TryAdd(catalog.Create(EquipmentSlot.Feet,EquipmentQuality.Common));
                grid.GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                var delete = content.Find("Backpack60/Inner/DeleteAction").GetComponent<UnityEngine.UI.Button>();
                delete.onClick.Invoke(); Check(g.equipment.Count == 1,"Delete requires confirmation",result);
                delete.onClick.Invoke(); Check(g.equipment.Count == 0 && Near(g.EffectiveDamage,82),"Delete affects selected bag item only",result);
                g.equipment.TryAdd(catalog.Create(EquipmentSlot.Module,EquipmentQuality.Epic));
                var clock=typeof(PlagueGame).GetField("fireClock",Hidden); clock.SetValue(g,.24f);
                g.EquipItem(0);
                Check(Near((float)clock.GetValue(g),.24f/1.4f),"Attack cooldown preserves progress",result);
                g.UnequipItem(EquipmentSlot.Module);
                Check(Near((float)clock.GetValue(g),.24f),"Unequip reverses cooldown scaling",result);
                g.Loot.Spawn(catalog.Create(EquipmentSlot.Feet,EquipmentQuality.Rare),new Vector2(2,0));
                float elapsed=g.Elapsed; g.Tick(.5f,Vector2.zero);
                Check(Near(g.Elapsed,elapsed) && g.Loot.Count==1,"Inventory pauses combat and pickup",result);
                g.SetEquipmentOpen(false);
                for(int i=0;i<30;i++)g.Tick(.02f,Vector2.zero);
                Check(g.Loot.Count==0 && g.equipment.Count==2,"Nearby loot attracts into inventory",result);
                while(!g.equipment.Full)g.equipment.TryAdd(catalog.Roll());
                g.Loot.Spawn(catalog.Create(EquipmentSlot.Feet,EquipmentQuality.Rare),new Vector2(1,0));
                g.Tick(.05f,Vector2.zero);
                Check(g.Loot.Count==1 && g.Loot.NearbyBagFull,"Full bag leaves drop on ground with HUD notice",result);
                g.SetEquipmentOpen(true);g.DeleteInventoryItem(5);g.SetEquipmentOpen(false);
                for(int i=0;i<20;i++)g.Tick(.02f,Vector2.zero);
                Check(g.Loot.Count==0 && g.equipment.Full,"Pickup resumes after space is freed",result);

                g.city=city;g.Restart();
                if(city)
                {
                    var oldObstacles=city.obstacles;
                    try
                    {
                        city.obstacles=new[]{new Rect(.7f,-1,.3f,2)};
                        g.Loot.Spawn(catalog.Roll(),new Vector2(1.5f,0));
                        for(int i=0;i<30;i++)g.Loot.Tick(.02f,Vector2.zero);
                        Check(g.Loot.Count==1 && g.equipment.Count==0,"Loot does not attract through walls",result);
                    }
                    finally { city.obstacles=oldObstacles; }
                }
                g.city=null;g.enemyLimit=1;catalog.dropChance=0;
                var listField=typeof(PlagueGame).GetField("enemies",Hidden);
                float[] remaining = new float[2];
                for(int run=0;run<2;run++)
                {
                    g.Restart();
                    if(run==1) { g.equipment.TryAdd(catalog.Create(EquipmentSlot.Weapon,EquipmentQuality.Legendary));g.SetEquipmentOpen(true);g.EquipItem(0);g.SetEquipmentOpen(false); }
                    var enemies=(IList)listField.GetValue(g); var enemy=enemies[0];var type=enemy.GetType();
                    type.GetField("health").SetValue(enemy,100f); type.GetField("age").SetValue(enemy,1f);type.GetField("position").SetValue(enemy,new Vector2(2,0));
                    clock.SetValue(g,0f);
                    for(int i=0;i<25;i++)g.Tick(.01f,Vector2.zero);
                    remaining[run]=(float)type.GetField("health").GetValue(enemy);
                }
                Check(Near(remaining[0],78) && Near(remaining[1],18),"Projectile damage: 100 HP -> 78 base / 18 legendary",result);
                g.Restart();catalog.dropChance=1;catalog.magnetRadius=.1f;
                var enemyList=(IList)listField.GetValue(g);var victim=enemyList[0];var victimType=victim.GetType();
                victimType.GetField("health").SetValue(victim,1f);victimType.GetField("age").SetValue(victim,1f);victimType.GetField("position").SetValue(victim,new Vector2(2,0));clock.SetValue(g,0f);
                for(int i=0;i<25;i++)g.Tick(.01f,Vector2.zero);
                Check(g.Kills==1 && g.Loot.Count==1,"Actual enemy death creates one equipment drop",result);
                g.Restart();
                Check(Near(g.GetEnemyHealth(false),58) && Near(g.GetEnemyHealth(true),82),"Fixed enemy HP: melee 58 / ranged 82",result);
                var time=typeof(PlagueGame).GetField("<Elapsed>k__BackingField",Hidden);time.SetValue(g,120f);
                bool fixedHealth=true;
                for(int i=0;i<100;i++) fixedHealth &= Near(g.GetEnemyHealth(false),58) && Near(g.GetEnemyHealth(true),82);
                Check(fixedHealth,"Enemy HP has no time scaling or random variation",result);
                g.SetEquipmentOpen(true);g.DamagePlayer(1000);
                Check(!g.EquipmentOpen && !g.EquipItem(0),"Death closes inventory and blocks actions",result);
                g.Restart();
                Check(g.equipment.Count==0 && g.Loot.Count==0 && Near(g.EffectiveDamage,22),"Restart clears loot, inventory and loadout",result);
            }
            finally
            {
                g.city=city;g.enemyLimit=limit;catalog.dropChance=dropChance;catalog.magnetRadius=magnetRadius;
                UnityEngine.Random.state=randomState;g.Restart();g.SetEquipmentOpen(true);g.enabled=enabled;
            }
            return result.ToArray();
        }
    }
}
