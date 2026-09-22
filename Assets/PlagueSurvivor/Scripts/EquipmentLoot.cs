using System.Collections.Generic;
using UnityEngine;

namespace PlagueSurvivor
{
    public sealed class EquipmentLoot : MonoBehaviour
    {
        sealed class Drop
        {
            public EquipmentItem item;
            public Transform root, beam;
            public Vector2 position;
            public float phase;
        }
        readonly List<Drop> drops = new List<Drop>();
        PlagueGame game;
        Transform container;
        readonly Dictionary<EquipmentQuality, Mesh> coloredBeams = new Dictionary<EquipmentQuality, Mesh>();
        public int Count { get { return drops.Count; } }
        public bool Occupied(Vector2 position, float radius)
        { foreach (var drop in drops) if (Vector2.Distance(drop.position, position) < radius) return true; return false; }
        public bool NearbyBagFull { get; private set; }
        public void Initialize(PlagueGame owner) { game = owner; container = new GameObject("Equipment Drops").transform; }
        public void TryDrop(Vector2 position)
        {
            if (game.equipmentCatalog && Random.value < game.equipmentCatalog.dropChance) Spawn(game.equipmentCatalog.Roll(), position);
        }
        public void Spawn(EquipmentItem item, Vector2 position)
        {
            if (item == null || !game.equipmentCatalog) return;
            var catalog = game.equipmentCatalog;
            var root = new GameObject(item.Quality + " " + item.Name).transform;
            root.SetParent(container); root.position = position;
            var icon = new GameObject("Icon").AddComponent<SpriteRenderer>();
            icon.transform.SetParent(root, false); icon.transform.localScale = Vector3.one * .7f;
            icon.sprite = catalog.Icon(item); icon.sharedMaterial = catalog.worldMaterial; icon.sortingOrder = 450;
            var beam = new GameObject("Quality Beam", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            beam.SetParent(root, false);
            Mesh mesh;
            if (!coloredBeams.TryGetValue(item.Quality, out mesh))
            {
                mesh = new Mesh { name = item.Quality + " Loot Beam" };
                mesh.vertices = new[] { new Vector3(-.16f,0,0), new Vector3(.16f,0,0), new Vector3(-.38f,2.4f,0), new Vector3(.38f,2.4f,0) };
                mesh.triangles = new[] { 0,2,1, 2,3,1 };
                var c = catalog.Tier(item.Quality).color;
                mesh.colors = new[] { new Color(c.r,c.g,c.b,.8f), new Color(c.r,c.g,c.b,.8f), new Color(c.r,c.g,c.b,0), new Color(c.r,c.g,c.b,0) };
                mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
                mesh.RecalculateBounds(); coloredBeams.Add(item.Quality, mesh);
            }
            beam.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = beam.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = catalog.worldMaterial; renderer.sortingOrder = 449;
            drops.Add(new Drop { item = item, root = root, beam = beam, position = position, phase = Random.value * 6 });
        }
        public void Tick(float dt, Vector2 playerPosition)
        {
            NearbyBagFull = false;
            var catalog = game.equipmentCatalog;
            if (!catalog) return;
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var drop = drops[i];
                drop.phase += dt * 2;
                drop.beam.localScale = new Vector3(1 + Mathf.Sin(drop.phase) * .10f, 1, 1);
                if (Vector2.Distance(drop.position, playerPosition) > catalog.magnetRadius) continue;
                if (game.equipment.Full) { NearbyBagFull = true; continue; }
                if (game.city && !game.city.LineClear(drop.position, playerPosition, .1f)) continue;
                drop.position = Vector2.MoveTowards(drop.position, playerPosition, catalog.magnetSpeed * dt);
                drop.root.position = drop.position;
                if (Vector2.Distance(drop.position, playerPosition) <= catalog.pickupRadius && game.equipment.TryAdd(drop.item))
                { Destroy(drop.root.gameObject); drops.RemoveAt(i); }
            }
        }
        public void Clear()
        {
            foreach (var drop in drops) if (drop.root) { drop.root.gameObject.SetActive(false); Destroy(drop.root.gameObject); }
            drops.Clear(); NearbyBagFull = false;
        }
        void OnDestroy()
        {
            if (container) Destroy(container.gameObject);
            foreach (var mesh in coloredBeams.Values) if (mesh) Destroy(mesh);
        }
    }
}
