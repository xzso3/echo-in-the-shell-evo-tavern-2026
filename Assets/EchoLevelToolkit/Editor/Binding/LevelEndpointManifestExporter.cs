using System;
using System.Collections.Generic;
using System.IO;
using Echo.LevelToolkit.Binding;
using UnityEditor;
using UnityEngine;

namespace Echo.LevelToolkit.Editor.Binding
{
    public static class LevelEndpointManifestExporter
    {
        [MenuItem("Assets/Echo/Level Toolkit/Export Endpoint Manifest", true)]
        private static bool CanExport()
        {
            return Selection.activeObject is LevelEndpointCatalog;
        }

        [MenuItem("Assets/Echo/Level Toolkit/Export Endpoint Manifest")]
        private static void Export()
        {
            var catalog = Selection.activeObject as LevelEndpointCatalog;
            if (catalog == null) return;
            IReadOnlyList<string> errors = catalog.ValidateCatalog();
            if (errors.Count != 0)
            {
                EditorUtility.DisplayDialog("Endpoint manifest invalid", string.Join("\n", errors), "OK");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(catalog);
            string folder = Path.GetDirectoryName(assetPath);
            string fileName = Path.GetFileNameWithoutExtension(assetPath) + ".endpoints";
            string outputPath = EditorUtility.SaveFilePanelInProject("Export Endpoint Manifest",
                fileName, "json", "Choose a path for the level interface manifest.", folder);
            if (string.IsNullOrEmpty(outputPath)) return;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(projectRoot, outputPath);
            string nextJson = catalog.GenerateManifestJson();

            if (File.Exists(absolutePath))
            {
                IReadOnlyList<string> removed = RemovedEndpointIds(File.ReadAllText(absolutePath), nextJson);
                if (removed.Count != 0 && !EditorUtility.DisplayDialog("Endpoint IDs removed",
                    "Existing bindings that use these IDs need migration:\n" + string.Join("\n", removed),
                    "Export and migrate", "Cancel"))
                    return;
            }
            File.WriteAllText(absolutePath, nextJson);
            AssetDatabase.ImportAsset(outputPath);
            Debug.Log($"Endpoint manifest exported: {outputPath}", catalog);
        }

        private static IReadOnlyList<string> RemovedEndpointIds(string previousJson, string nextJson)
        {
            var removed = new List<string>();
            try
            {
                var previous = JsonUtility.FromJson<Manifest>(previousJson);
                var next = JsonUtility.FromJson<Manifest>(nextJson);
                var nextIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (ManifestEntry entry in next.endpoints) nextIds.Add(entry.id);
                foreach (ManifestEntry entry in previous.endpoints)
                    if (!nextIds.Contains(entry.id)) removed.Add(entry.id);
            }
            catch (Exception)
            {
                removed.Add("Previous manifest could not be read; inspect existing bindings manually.");
            }
            return removed;
        }

        [Serializable]
        private sealed class Manifest
        {
            public ManifestEntry[] endpoints;
        }

        [Serializable]
        private sealed class ManifestEntry
        {
            public string id;
        }
    }
}
