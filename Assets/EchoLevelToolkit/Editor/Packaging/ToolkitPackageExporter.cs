using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Packaging;
using UnityEditor;
using UnityEngine;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Echo.LevelToolkit.Editor.Packaging
{
    [Serializable]
    public sealed class ToolkitPackageIndex
    {
        public int schemaVersion = 1;
        public string toolkitVersion;
        public int contentFormatVersion;
        public string unityVersion;
        public string urpVersion;
        public string templateVersion;
        public PackageRequirement[] packages;
        public PackageAsset[] assets;
        public PackageAsset[] folders;
        public string packageSha256;
    }

    public static class ToolkitPackageExporter
    {
        [MenuItem("Echo/Level Toolkit/Export Toolkit Package")]
        private static void ExportMenu()
        {
            try
            {
                var errors = new List<string>();
                ToolkitVersion profile = PackagingVersion.FindProfile(errors);
                if (profile == null) throw new InvalidOperationException(string.Join("\n", errors));
                string output = EditorUtility.SaveFilePanel("Export toolkit", "", "EchoLevelToolkit-" + profile.Version + ".unitypackage", "unitypackage");
                if (string.IsNullOrEmpty(output)) return;
                ToolkitPackageIndex index = ExportTo(output);
                Debug.Log("Toolkit package exported: " + output + " (" + index.assets.Length + " assets; manifest: " + output + ".manifest.json)");
            }
            catch (Exception ex) { EditorUtility.DisplayDialog("Toolkit export failed", ex.Message, "OK"); Debug.LogException(ex); }
        }

        // Explicit asset list, no IncludeDependencies. Also callable from a batch driver.
        public static ToolkitPackageIndex ExportTo(string outputPath)
        {
            var errors = new List<string>();
            ToolkitVersion profile = PackagingVersion.FindProfile(errors);
            if (profile == null) throw new InvalidOperationException(string.Join("\n", errors));
            if (Application.unityVersion != profile.UnityVersion) throw new InvalidOperationException("Unity version differs from toolkit profile.");
            string[] paths = AssetDatabase.GetAllAssetPaths().Where(x => x.StartsWith(PackagePolicy.ToolkitRoot, StringComparison.Ordinal)
                && !x.StartsWith(PackagePolicy.ManifestRoot, StringComparison.Ordinal)
                && !x.StartsWith("Assets/EchoLevelToolkit/Editor/Packaging/AcceptanceRecords/", StringComparison.Ordinal))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (paths.Length == 0) throw new InvalidOperationException("Toolkit has no assets.");
            string[] filePaths = paths.Where(x => !AssetDatabase.IsValidFolder(x)).ToArray();
            string[] folderPaths = paths.Where(AssetDatabase.IsValidFolder).ToArray();
            var whitelist = profile.Packages.ToDictionary(x => x.PackageId, x => x.ExactVersion, StringComparer.Ordinal);
            foreach (string dependency in AssetDatabase.GetDependencies(filePaths, true))
            {
                if (dependency.StartsWith("Packages/", StringComparison.Ordinal)
                    && !dependency.StartsWith("Packages/com.unity.modules.", StringComparison.Ordinal))
                {
                    UpmPackageInfo info = UpmPackageInfo.FindForAssetPath(dependency);
                    if (info == null || !whitelist.TryGetValue(info.name, out string exact) || exact != info.version)
                        errors.Add("Toolkit UPM dependency is outside exact whitelist: " + dependency);
                }
                else if (!dependency.StartsWith(PackagePolicy.ToolkitRoot, StringComparison.Ordinal)
                    && !dependency.StartsWith("Packages/", StringComparison.Ordinal)
                    && !dependency.StartsWith("Resources/unity_builtin_extra", StringComparison.Ordinal)
                    && !dependency.StartsWith("Library/unity default resources", StringComparison.Ordinal))
                    errors.Add("Toolkit has external dependency: " + dependency);
            }
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            string sidecar = outputPath + ".manifest.json";
            string staged = outputPath + ".staging.unitypackage";
            string stagedSidecar = sidecar + ".staging";
            if (File.Exists(outputPath) || File.Exists(sidecar) || File.Exists(staged) || File.Exists(stagedSidecar))
                throw new IOException("Output or staging file already exists.");
            try
            {
                AssetDatabase.ExportPackage(paths, staged, ExportPackageOptions.Default);
                Dictionary<string, UnityPackageArchive.Asset> archive = UnityPackageArchive.Read(staged);
                var actual = archive.Values.Where(x => !x.isFolder).ToDictionary(x => x.path, x => x, StringComparer.Ordinal);
                if (actual.Keys.Any(x => !x.StartsWith(PackagePolicy.ToolkitRoot, StringComparison.Ordinal)
                    || x.StartsWith(PackagePolicy.ManifestRoot, StringComparison.Ordinal)
                    || x.StartsWith("Assets/EchoLevelToolkit/Editor/Packaging/AcceptanceRecords/", StringComparison.Ordinal)))
                    throw new InvalidDataException("Toolkit package contains non-public or generated assets.");
                if (actual.Count != filePaths.Length || filePaths.Any(x => !actual.ContainsKey(x)))
                    throw new InvalidDataException("Toolkit package differs from explicit asset list.");
                var actualFolders = archive.Values.Where(x => x.isFolder).ToDictionary(x => x.path, x => x, StringComparer.Ordinal);
                if (actualFolders.Keys.Any(x => x != "Assets" && x != "Assets/EchoLevelToolkit"
                    && !x.StartsWith(PackagePolicy.ToolkitRoot, StringComparison.Ordinal))
                    || folderPaths.Any(x => !actualFolders.ContainsKey(x)))
                    throw new InvalidDataException("Toolkit package folder metadata differs from explicit folder list.");
                var index = new ToolkitPackageIndex
                {
                    toolkitVersion = profile.Version, contentFormatVersion = profile.ContentFormatVersion,
                    unityVersion = profile.UnityVersion, urpVersion = profile.UrpVersion,
                    templateVersion = profile.TemplateVersion,
                    packages = profile.Packages.Select(x => new PackageRequirement { packageId = x.PackageId, exactVersion = x.ExactVersion }).ToArray(),
                    assets = actual.Values.OrderBy(x => x.path, StringComparer.Ordinal).Select(x => new PackageAsset
                    {
                        path = x.path, guid = x.guid, assetSha256 = x.content.sha256, metaSha256 = x.meta.sha256
                    }).ToArray(),
                    folders = actualFolders.Values.OrderBy(x => x.path, StringComparer.Ordinal).Select(x => new PackageAsset
                    {
                        path = x.path, guid = x.guid, metaSha256 = x.meta.sha256
                    }).ToArray(),
                    packageSha256 = PackagePolicy.Sha256File(staged)
                };
                File.WriteAllText(stagedSidecar, JsonUtility.ToJson(index, true));
                File.Move(staged, outputPath);
                try { File.Move(stagedSidecar, sidecar); }
                catch { File.Delete(outputPath); throw; }
                return index;
            }
            finally
            {
                if (File.Exists(staged)) File.Delete(staged);
                if (File.Exists(stagedSidecar)) File.Delete(stagedSidecar);
            }
        }
    }
}
