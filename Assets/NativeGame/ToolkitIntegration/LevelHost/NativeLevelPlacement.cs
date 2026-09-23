using System.Collections.Generic;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Level;
using Echo.LevelToolkit.Map;
using Echo.LevelToolkit.Map.Doors;
using UnityEngine;

namespace Echo.NativeGame.ToolkitIntegration.LevelHost
{
    // The root of an explicitly selected work prefab. Runtime IDs live on NativeLevelInstance,
    // never on this authored component or its ScriptableObject catalog.
    public sealed class NativeLevelPlacement : MonoBehaviour
    {
        [SerializeField] private LevelStage stage;
        [SerializeField] private ChunkMap map;
        [SerializeField] private MapDoorSet doors;
        [SerializeField] private CombatWorld world;
        [SerializeField] private CombatPlayer player;
        [SerializeField] private CombatWeapon weapon;
        [SerializeField] private LevelEndpointCatalog catalog;
        [Header("Camera")]
        [SerializeField] private bool useExplicitCameraBounds;
        [SerializeField] private Vector2 cameraWorldCenter;
        [SerializeField] private Vector2 cameraWorldHalfSize = new Vector2(15, 9);
        [SerializeField, Min(0)] private float cameraPadding = 2;

        public LevelStage Stage => stage;
        public ChunkMap Map => map;
        public MapDoorSet Doors => doors;
        public CombatWorld World => world;
        public CombatPlayer Player => player;
        public CombatWeapon Weapon => weapon;
        public LevelEndpointCatalog Catalog => catalog;

        public bool Validate(out string diagnostic)
        {
            if (!stage || !map || !doors || !world || !player || !catalog)
            { diagnostic = "Assign Stage, ChunkMap, MapDoorSet, CombatWorld, CombatPlayer and catalog."; return false; }
            if (!stage.LevelId.IsComplete || !stage.LevelId.Equals(catalog.LevelIdentity) || stage.Doors != doors)
            { diagnostic = "Stage, door set and endpoint catalog do not describe the same level."; return false; }
            if (!Contains(stage.transform) || !Contains(map.transform) || !Contains(doors.transform)
                || !Contains(world.transform) || !Contains(player.transform)
                || weapon && !Contains(weapon.transform))
            { diagnostic = "Every runtime component must belong to this placement root."; return false; }
            if (!stage.PlayerSpawn || !Contains(stage.PlayerSpawn))
            { diagnostic = "A player spawn inside the placement is required."; return false; }
            if (weapon && weapon.GetComponent<CombatPlayer>() != player)
            { diagnostic = "Weapon must be attached to the assigned CombatPlayer."; return false; }
            IReadOnlyList<string> catalogErrors = catalog.ValidateCatalog();
            if (catalogErrors.Count != 0)
            { diagnostic = "Endpoint catalog is invalid: " + string.Join("; ", catalogErrors); return false; }
            if (!TryGetCameraBounds(out _, out _))
            { diagnostic = "Camera bounds are invalid or the map has no valid placements."; return false; }
            diagnostic = string.Empty;
            return true;
        }

        public bool TryGetCameraBounds(out Vector2 center, out Vector2 halfSize)
        {
            center = cameraWorldCenter;
            halfSize = cameraWorldHalfSize;
            if (useExplicitCameraBounds)
                return Finite(center) && Finite(halfSize) && halfSize.x > 0 && halfSize.y > 0;
            if (!map || !Finite(cameraPadding) || cameraPadding < 0) return false;
            List<ChunkPlacement> placements = map.GetPlacements();
            if (placements.Count == 0) return false;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (ChunkPlacement placement in placements)
            {
                if (!placement.Chunk || !placement.Chunk.HasValidMetadata) return false;
                Vector3 a = map.transform.TransformPoint(map.CellToLocal(placement.CellOrigin));
                Vector3 b = map.transform.TransformPoint(map.CellToLocal(
                    placement.CellOrigin + Vector2Int.one * placement.Chunk.Size));
                min = Vector2.Min(min, Vector2.Min(a, b));
                max = Vector2.Max(max, Vector2.Max(a, b));
            }
            center = (min + max) * .5f;
            halfSize = (max - min) * .5f + Vector2.one * cameraPadding;
            return Finite(center) && Finite(halfSize) && halfSize.x > 0 && halfSize.y > 0;
        }

        // Emitters are optional physical/interaction sources. They bind only after the
        // Integrated session starts; their owning components decide when Raise is real.
        public bool BindEmitters(LevelBindingSession session, RuntimeScope scope)
        {
            foreach (LevelEventEmitter emitter in GetComponentsInChildren<LevelEventEmitter>(true))
            {
                if (emitter.Bind(session, scope)) continue;
                UnbindEmitters();
                return false;
            }
            return true;
        }

        public void UnbindEmitters()
        {
            foreach (LevelEventEmitter emitter in GetComponentsInChildren<LevelEventEmitter>(true))
                if (emitter) emitter.Unbind();
        }

        private bool Contains(Transform child) => child && (child == transform || child.IsChildOf(transform));
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
