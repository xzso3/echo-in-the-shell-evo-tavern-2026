using System.Collections.Generic;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Level;
using Echo.LevelToolkit.Map.Doors;
using UnityEditor;
using UnityEngine;

namespace Echo.LevelToolkit.Level.Editor
{
    [CustomEditor(typeof(LevelStage))]
    public sealed class LevelStageInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Edit each encounter's PolygonCollider2D outline with Unity's collider tool. " +
                "One outline may cross any number of Chunk instances. KT-04 checks physical exit coverage.",
                MessageType.Info);
            if (GUILayout.Button("Validate encounter configuration"))
            {
                var stage = (LevelStage)target;
                List<string> errors = LevelStageAuthoring.Validate(stage);
                if (errors.Count == 0)
                    EditorUtility.DisplayDialog("Level encounters", "Local encounter configuration is valid. " +
                        "Run KT-04 spatial validation and a Play Mode short flow before delivery.", "OK");
                else
                    EditorUtility.DisplayDialog("Level encounters", string.Join("\n", errors), "OK");
            }
        }
    }

    public static class LevelStageAuthoring
    {
        [MenuItem("Echo/Level Toolkit/Validate Selected Level Encounters")]
        private static void ValidateSelected()
        {
            LevelStage stage = Selection.activeGameObject
                ? Selection.activeGameObject.GetComponentInParent<LevelStage>() : null;
            if (!stage)
            {
                EditorUtility.DisplayDialog("Level encounters", "Select a LevelStage or one of its children.", "OK");
                return;
            }
            List<string> errors = Validate(stage);
            EditorUtility.DisplayDialog("Level encounters",
                errors.Count == 0 ? "Local encounter configuration is valid. Run KT-04 spatial validation."
                    : string.Join("\n", errors), "OK");
        }

        public static List<string> Validate(LevelStage stage)
        {
            var errors = new List<string>();
            if (!stage) { errors.Add("KT05_LEVEL_MISSING: Missing LevelStage."); return errors; }
            if (!stage.LevelId.IsComplete) errors.Add("KT05_ID: Level identity is incomplete.");
            if (!stage.Doors) errors.Add("KT05_DOOR_SET: Assign MapDoorSet.");
            if (!stage.PlayerSpawn) errors.Add("KT05_SPAWN: Assign a player spawn point.");

            var doorIds = new HashSet<ContentIdentity>();
            if (stage.Doors)
                foreach (MapDoor door in stage.Doors.ConfiguredDoors)
                {
                    if (!door || !door.DoorId.IsComplete || !door.Blocker)
                    { errors.Add("KT05_DOOR_CONFIG: A map door is null or lacks identity/blocker."); continue; }
                    if (!doorIds.Add(door.DoorId)) errors.Add("KT05_DOOR_ID: Duplicate map door ID: " + door.DoorId);
                }
            var owners = new Dictionary<ContentIdentity, ContentIdentity>();
            var actorOwners = new HashSet<CombatEnemy>();
            var bossOwners = new HashSet<CombatBoss>();
            var encounterIds = new HashSet<ContentIdentity>();
            var regions = new List<Vector2[]>();
            foreach (LevelEncounter encounter in stage.Encounters)
            {
                if (!encounter) { errors.Add("KT05_ENCOUNTER_MISSING: Null encounter reference."); continue; }
                string label = encounter.name;
                if (encounter.Mode != EncounterMode.FreeCombat && encounter.Mode != EncounterMode.ClearEnemies
                    && encounter.Mode != EncounterMode.Boss)
                    errors.Add("KT05_MODE: " + label + ": unknown encounter mode.");
                if (!encounter.EncounterId.IsComplete || !encounterIds.Add(encounter.EncounterId))
                    errors.Add("KT05_ID: " + label + ": identity is incomplete or duplicated.");
                if (!encounter.RegionEnteredEndpoint.IsComplete)
                    errors.Add("KT05_ENDPOINT: " + label + ": RegionEntered endpoint is missing.");
                if (encounter.Mode == EncounterMode.FreeCombat)
                {
                    if (encounter.SealDoors.Count != 0) errors.Add("KT05_MODE: " + label + ": free combat cannot seal doors.");
                    if (encounter.Boss) errors.Add("KT05_MODE: " + label + ": free combat cannot own a Boss.");
                }
                else
                {
                    if (!encounter.ActivateEndpoint.IsComplete || !encounter.CompletedEndpoint.IsComplete)
                        errors.Add("KT05_ENDPOINT: " + label + ": activation or completion endpoint is missing.");
                }
                if (encounter.Mode == EncounterMode.ClearEnemies && encounter.Enemies.Count == 0)
                    errors.Add("KT05_TARGET: " + label + ": clear encounter needs at least one enemy.");
                if (encounter.Mode == EncounterMode.Boss && (!encounter.Boss || encounter.Enemies.Count != 0))
                    errors.Add("KT05_TARGET: " + label + ": Boss encounter needs one Boss and no normal enemies.");
                if (encounter.Boss && !bossOwners.Add(encounter.Boss))
                    errors.Add("KT05_TARGET_DUPLICATE: " + label + ": Boss assigned to multiple encounters.");
                foreach (CombatEnemy enemy in encounter.Enemies)
                    if (!enemy || !actorOwners.Add(enemy))
                        errors.Add("KT05_TARGET_DUPLICATE: " + label + ": enemy missing or assigned to multiple encounters.");
                foreach (ContentIdentity doorId in encounter.SealDoors)
                {
                    if (!doorIds.Contains(doorId)) errors.Add("KT05_DOOR_MISSING: " + label + ": unknown seal door " + doorId);
                    else if (owners.TryGetValue(doorId, out var owner) && !owner.Equals(encounter.EncounterId))
                        errors.Add("KT05_DOOR_OWNER: " + label + ": door " + doorId + " already belongs to " + owner);
                    else owners[doorId] = encounter.EncounterId;
                }
                PolygonCollider2D area = encounter.GetComponent<PolygonCollider2D>();
                if (!area || !area.isTrigger || area.pathCount != 1 || area.GetPath(0).Length < 3)
                { errors.Add("KT05_REGION_SHAPE: " + label + ": use one PolygonCollider2D trigger path with at least three vertices."); continue; }
                Vector2[] points = WorldPoints(area);
                if (SelfIntersects(points)) errors.Add("KT05_REGION_SELF_INTERSECT: " + label + ": area polygon self-intersects.");
                foreach (Vector2[] other in regions)
                    if (Overlaps(points, other)) { errors.Add("KT05_REGION_OVERLAP: " + label + ": area overlaps another encounter."); break; }
                regions.Add(points);
            }
            if (stage.Exits.Count == 0) errors.Add("KT05_EXIT_MISSING: Assign at least one LevelExit.");
            foreach (LevelExit exit in stage.Exits)
                if (!exit || !exit.ExitId.IsComplete || !exit.ReachedEndpoint.IsComplete
                    || !exit.UnlockEndpoint.IsComplete)
                    errors.Add("KT05_EXIT_CONFIG: Exit is missing or has incomplete IDs.");
            return errors;
        }

        private static Vector2[] WorldPoints(PolygonCollider2D collider)
        {
            Vector2[] path = collider.GetPath(0);
            var points = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++)
                points[i] = collider.transform.TransformPoint(path[i] + collider.offset);
            return points;
        }

        private static bool SelfIntersects(Vector2[] polygon)
        {
            for (int i = 0; i < polygon.Length; i++)
                for (int j = i + 1; j < polygon.Length; j++)
                {
                    if (j == i + 1 || i == 0 && j == polygon.Length - 1) continue;
                    if (ProperlyCrosses(polygon[i], polygon[(i + 1) % polygon.Length],
                        polygon[j], polygon[(j + 1) % polygon.Length])) return true;
                }
            return false;
        }

        private static bool Overlaps(Vector2[] a, Vector2[] b)
        {
            for (int i = 0; i < a.Length; i++)
                for (int j = 0; j < b.Length; j++)
                    if (ProperlyCrosses(a[i], a[(i + 1) % a.Length], b[j], b[(j + 1) % b.Length]))
                        return true;
            foreach (Vector2 point in a) if (StrictlyInside(point, b)) return true;
            foreach (Vector2 point in b) if (StrictlyInside(point, a)) return true;
            float left = float.NegativeInfinity, right = float.PositiveInfinity;
            float bottom = float.NegativeInfinity, top = float.PositiveInfinity;
            Vector2 aMin = Min(a), aMax = Max(a), bMin = Min(b), bMax = Max(b);
            left = Mathf.Max(aMin.x, bMin.x); right = Mathf.Min(aMax.x, bMax.x);
            bottom = Mathf.Max(aMin.y, bMin.y); top = Mathf.Min(aMax.y, bMax.y);
            if (left < right && bottom < top)
            {
                Vector2 center = new Vector2((left + right) * .5f, (bottom + top) * .5f);
                if (StrictlyInside(center, a) && StrictlyInside(center, b)) return true;
            }
            return false;
        }

        private static Vector2 Min(Vector2[] points)
        {
            Vector2 value = points[0];
            foreach (Vector2 point in points)
            { value.x = Mathf.Min(value.x, point.x); value.y = Mathf.Min(value.y, point.y); }
            return value;
        }

        private static Vector2 Max(Vector2[] points)
        {
            Vector2 value = points[0];
            foreach (Vector2 point in points)
            { value.x = Mathf.Max(value.x, point.x); value.y = Mathf.Max(value.y, point.y); }
            return value;
        }

        private static bool ProperlyCrosses(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float abC = Cross(b - a, c - a), abD = Cross(b - a, d - a);
            float cdA = Cross(d - c, a - c), cdB = Cross(d - c, b - c);
            return abC * abD < -0.000001f && cdA * cdB < -0.000001f;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool StrictlyInside(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[j], b = polygon[i];
                float cross = Cross(b - a, point - a);
                if (Mathf.Abs(cross) < .0001f && Vector2.Dot(point - a, point - b) <= 0)
                    return false;
                if ((a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
