using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Echo.NativeGame.Editor
{
    // Explicit authoring of the one current playable and its own prefabs. No runtime scan or global font fallback.
    public static class NativeFontSetup
    {
        public const string FontPath = "Assets/NativeGame/Fonts/FusionPixel/FusionPixel12 Bitmap.asset";
        public const string SourcePath = "Assets/NativeGame/Fonts/FusionPixel/fusion-pixel-12px-proportional-zh_hans.otf";
        static readonly HashSet<char> characters = new HashSet<char>();
        public static void ApplyChineseAndFont()
        { NativeChineseTextMigration.Apply(); Apply(); }
        [MenuItem("Echo/Native/Apply FusionPixel To Current Playable")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (!TMP_Settings.instance) throw new InvalidOperationException("TMP essential resources are missing.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            if (!font || !source || font.sourceFontFile != source || !font.material || !font.material.shader)
                throw new InvalidOperationException("FusionPixel font/source/material/shader dependency did not import correctly.");
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic; font.isMultiAtlasTexturesEnabled = true;
            font.fallbackFontAssetTable.Clear();
            characters.Clear(); Add("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz []()/+-:.,!?%\\n");
            foreach (var path in Directory.GetFiles("Assets/NativeGame", "*.cs", SearchOption.AllDirectories))
            {
                if (path.Contains("NativeFontSetup") || path.Contains("Temp")) continue;
                foreach (Match match in Regex.Matches(File.ReadAllText(path), "\"(?:\\\\.|[^\"\\\\])*\"")) Add(match.Value);
            }
            int prefabLabels = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/NativeGame" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { int count = ApplyRoot(root, font, source); prefabLabels += count; if (count > 0) PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var scene = EditorSceneManager.OpenScene(NativeDemoBuilder.ScenePath);
            int sceneLabels = 0;
            foreach (var root in scene.GetRootGameObjects()) sceneLabels += ApplyRoot(root, font, source);
            foreach (var node in UnityEngine.Object.FindObjectsOfType<NativeMemoryNode>()) { Add(node.title); Add(node.body); }
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            TuneChineseLayout(run);
            Add(run.quest.initialObjective); Add(run.quest.completedObjective); Add(run.rules.terminalMessage);
            foreach (var interaction in run.hud.interactables) if (interaction) Add(interaction.promptOverride);
            string all = new string(characters.OrderBy(c => c).ToArray());
            string needed = new string(all.Where(c => !font.HasCharacter(c)).ToArray());
            if (needed.Length > 0) font.TryAddCharacters(needed);
            if (!font.HasCharacters(all, out uint[] missing, false, false)) throw new InvalidOperationException("FusionPixel missing source characters: " + string.Join(",", missing));
            foreach (var atlas in font.atlasTextures)
            {
                atlas.filterMode = FilterMode.Point; atlas.anisoLevel = 0;
                if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
                EditorUtility.SetDirty(atlas);
            }
            if (!AssetDatabase.Contains(font.material)) AssetDatabase.AddObjectToAsset(font.material, font);
            font.material.mainTexture = font.atlasTexture;
            EditorUtility.SetDirty(font.material); EditorUtility.SetDirty(font);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            var saved = AssetDatabase.LoadAllAssetsAtPath(FontPath);
            if (!saved.OfType<Material>().Any() || saved.OfType<Texture2D>().Count() != font.atlasTextures.Length)
                throw new InvalidOperationException("Font material or atlas subassets missing on disk.");
            var dependencies = AssetDatabase.GetDependencies(NativeDemoBuilder.ScenePath, true);
            if (!dependencies.Contains(FontPath) || !dependencies.Contains(SourcePath)) throw new InvalidOperationException("Playable does not include the bundled TMP font and OTF source.");
            Debug.Log("NATIVE_FONT_SETUP: " + sceneLabels + " scene labels, " + prefabLabels + " prefab labels; warmed " + characters.Count + " unique source/text characters; missing=0; saved atlas=" + font.atlasTextures.Length + "; source=" + AssetDatabase.AssetPathToGUID(SourcePath));
        }
        public static void FinalizeChineseLayout()
        {
            Apply();
            var allowed = new HashSet<string> { "WASD", "Space", "Enter", "Tab" };
            var leftovers = new List<string>();
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                    foreach (Match token in Regex.Matches(label.text ?? "", "[A-Za-z]{2,}"))
                        if (!allowed.Contains(token.Value)) leftovers.Add(label.name + ": " + token.Value);
            if (leftovers.Count > 0) throw new InvalidOperationException("Untranslated authored text: " + string.Join("; ", leftovers));
            Debug.Log("NATIVE_ZH_AUTHORED: all active/inactive scene text checked; remaining Latin words are key names only.");
        }
        static void TuneChineseLayout(NativeRunController run)
        {
            var phone = run.hud.phone;
            foreach (var label in new[] { run.hud.healthLabel, run.hud.fireLabel, run.hud.counterLabel, run.hud.transform.Find("Title").GetComponent<TMP_Text>() })
                label.rectTransform.sizeDelta = new Vector2(label.rectTransform.sizeDelta.x, 33);
            run.hud.counterLabel.rectTransform.anchoredPosition = new Vector2(0, 3);
            run.endingSequence.caption.rectTransform.sizeDelta = new Vector2(800, 160);
            run.hud.phonePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 500);
            phone.heading.rectTransform.sizeDelta = new Vector2(564, 32);
            phone.scroll.viewport.sizeDelta = new Vector2(564, 190);
            phone.body.rectTransform.sizeDelta = new Vector2(552, 190);
            for (int i = 0; i < phone.tabs.Length; i++)
                SizeButton(phone.tabs[i], new Vector2(18 + i * 190, -54), new Vector2(184, 34));
            for (int i = 0; i < phone.actions.Length; i++)
                SizeButton(phone.actions[i], new Vector2(18 + (i % 2) * 286, 58 + (3 - i / 2) * 36), new Vector2(278, 32));
            SizeButton(run.hud.phoneCloseButton, new Vector2(18, 14), new Vector2(278, 32));
            SizeButton(phone.restart, new Vector2(304, 14), new Vector2(278, 32));
            run.dialogue.panel.GetComponent<RectTransform>().sizeDelta = new Vector2(780, 310);
            run.dialogue.message.rectTransform.sizeDelta = new Vector2(732, 234);
            run.hud.objectiveLabel.rectTransform.anchoredPosition = new Vector2(24, -52);
            run.hud.objectiveLabel.rectTransform.sizeDelta = new Vector2(750, 64);
        }
        static void SizeButton(UnityEngine.UI.Button button, Vector2 at, Vector2 size)
        {
            var rect = button.GetComponent<RectTransform>(); rect.anchoredPosition = at; rect.sizeDelta = size;
            button.GetComponentInChildren<TMP_Text>(true).rectTransform.sizeDelta = size;
        }
        static void Add(string text)
        { if (text != null) foreach (char c in text) if (!char.IsControl(c) && !char.IsSurrogate(c) && c != '\uFEFF') characters.Add(c); }
        static int ApplyRoot(GameObject root, TMP_FontAsset font, Font source)
        {
            int count = 0;
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                Add(label.text); label.font = font; label.fontSharedMaterial = font.material;
                label.enableAutoSizing = false;
                if (label is TextMeshPro) label.fontSize = 4.8f;
                else if (label.GetComponentInParent<NativeBossDisplay>()) label.fontSize = 36;
                else label.fontSize = label.fontSize >= 28 ? 36 : 24;
                EditorUtility.SetDirty(label); PrefabUtility.RecordPrefabInstancePropertyModifications(label); count++;
            }
            foreach (var label in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            { Add(label.text); label.font = source; label.resizeTextForBestFit = false; label.fontSize = 24; EditorUtility.SetDirty(label); PrefabUtility.RecordPrefabInstancePropertyModifications(label); count++; }
            foreach (var display in root.GetComponentsInChildren<NativeBossDisplay>(true))
            {
                var title = display.title.rectTransform; title.anchorMin = new Vector2(0, .6f); title.anchorMax = Vector2.one; title.offsetMin = title.offsetMax = Vector2.zero;
                var cue = display.cue.rectTransform; cue.anchorMin = Vector2.zero; cue.anchorMax = new Vector2(1, .4f); cue.offsetMin = cue.offsetMax = Vector2.zero;
                EditorUtility.SetDirty(title); EditorUtility.SetDirty(cue);
                PrefabUtility.RecordPrefabInstancePropertyModifications(title); PrefabUtility.RecordPrefabInstancePropertyModifications(cue);
            }
            return count;
        }
    }
}
