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
            return count;
        }
    }
}
