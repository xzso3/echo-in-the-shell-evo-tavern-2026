using System;
using PlagueSurvivor;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Echo.NativeGame.Editor
{
    public static class NativeBossPrefabBuilder
    {
        public const string PrefabPath = "Assets/NativeGame/Boss/DevelopmentBoss.prefab";
        [MenuItem("Echo/Native/Create Development Boss Prefab")]
        public static void Build() { Selection.activeObject = CreatePrefab(); }

        // Only writes this Boss prefab; never opens/saves a scene or modifies the main builder.
        public static GameObject CreatePrefab()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Build the Boss prefab outside Play mode.");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing) return existing;
            var art = AssetDatabase.LoadAssetAtPath<BossArenaConfig>("Assets/BossArena/BossArenaConfig.asset");
            var pulse = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NativeGame/Prefabs/Pulse.prefab");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (!art || !art.material || !art.baseSprite || !art.turret || !art.core || !art.wreck || !art.projectile || !pulse || !pulse.GetComponent<NativeProjectile>() || !font)
                throw new InvalidOperationException("Missing existing BossArena art, native Pulse prefab or TMP font.");
            var root = new GameObject("Development Boss");
            try
            {
                var actor = root.AddComponent<NativeBossActor>();
                var boss = root.AddComponent<NativeBossController>();
                root.GetComponent<CircleCollider2D>().radius = .9f;
                boss.actor = actor; boss.art = art; boss.hostileProjectilePrefab = pulse.GetComponent<NativeProjectile>();
                boss.baseView = Sprite("Shell", root.transform, art.baseSprite, art, 80);
                boss.turretPivot = new GameObject("Turret pivot").transform;
                boss.turretPivot.SetParent(root.transform, false);
                boss.turretView = Sprite("Turret", boss.turretPivot, art.turret, art, 82);
                boss.coreView = Sprite("Core", boss.turretPivot, art.core, art, 83);
                boss.damageView = Sprite("Shell damage", root.transform, art.damageOverlay, art, 84);
                boss.damageView.enabled = false;
                BuildDisplay(root, boss, font);
                var result = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (!result) throw new InvalidOperationException("Could not save Boss prefab.");
                return result;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static SpriteRenderer Sprite(string label, Transform parent, Sprite sprite, BossArenaConfig art, int order)
        {
            var view = new GameObject(label).AddComponent<SpriteRenderer>();
            view.transform.SetParent(parent, false); view.transform.localScale = Vector3.one * .8f;
            view.sprite = sprite; view.sharedMaterial = art.material; view.sortingOrder = order; return view;
        }
        static RectTransform Rect(string label, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        static TMP_Text Text(string label, Transform parent, TMP_FontAsset font, Vector2 min, Vector2 max, float size)
        {
            var rect = Rect(label, parent, min, max); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white; text.raycastTarget = false; text.text = label; return text;
        }
        static void BuildDisplay(GameObject root, NativeBossController boss, TMP_FontAsset font)
        {
            var rect = Rect("Boss display", root.transform, Vector2.zero, Vector2.zero);
            rect.sizeDelta = new Vector2(720, 125); rect.localPosition = new Vector3(0, 2.7f, 0); rect.localScale = Vector3.one * .01f;
            var canvas = rect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 500;
            var display = rect.gameObject.AddComponent<NativeBossDisplay>(); display.boss = boss; display.canvas = canvas;
            display.title = Text("DEVELOPMENT BOSS", rect, font, new Vector2(0, .65f), Vector2.one, 24);
            var track = Rect("Shell track", rect, new Vector2(.1f, .44f), new Vector2(.9f, .56f));
            var background = track.gameObject.AddComponent<UnityEngine.UI.Image>(); background.color = new Color(.07f, .11f, .15f, .95f); background.raycastTarget = false;
            display.armorFill = Rect("Shell remaining", track, Vector2.zero, Vector2.one);
            var fill = display.armorFill.gameObject.AddComponent<UnityEngine.UI.Image>(); fill.color = new Color(1, .65f, .15f); fill.raycastTarget = false;
            display.cue = Text("DISABLE THE OUTER SHELL", rect, font, Vector2.zero, new Vector2(1, .38f), 20);
        }
    }
}
