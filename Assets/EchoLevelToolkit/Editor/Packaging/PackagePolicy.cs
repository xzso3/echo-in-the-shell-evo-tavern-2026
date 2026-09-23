using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Echo.LevelToolkit.Packaging;
using UnityEditor;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace Echo.LevelToolkit.Editor.Packaging
{
    public enum DependencyClass { WorkOwned, ToolkitShared, UnityOrUpm, UndeclaredExternal }

    public sealed class DependencyItem
    {
        public string path;
        public DependencyClass category;
        public string packageId;
        public string packageVersion;
    }

    public sealed class DependencyScan
    {
        public readonly List<DependencyItem> items = new List<DependencyItem>();
        public readonly List<string> errors = new List<string>();
        public bool CanExport => errors.Count == 0;
        public IEnumerable<string> OwnedPaths => items.Where(x => x.category == DependencyClass.WorkOwned).Select(x => x.path);
        public IEnumerable<string> SharedPaths => items.Where(x => x.category == DependencyClass.ToolkitShared).Select(x => x.path);
    }

    public static class PackagePolicy
    {
        public const string ToolkitRoot = "Assets/EchoLevelToolkit/";
        public const string WorkRoot = "Assets/EchoUserContent/";
        public const string ManifestRoot = "Assets/EchoLevelToolkit/Editor/Packaging/WorkManifests/";
        private static readonly HashSet<string> ForbiddenWorkExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".js", ".boo", ".dll", ".asmdef", ".asmref", ".rsp", ".exe", ".so", ".dylib", ".bundle", ".sh", ".bat", ".cmd", ".py"
        };

        public static bool IsSafeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64) return false;
            foreach (char c in value)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z')
                    && !(c >= '0' && c <= '9') && c != '_' && c != '-') return false;
            return true;
        }

        public static bool IsSafeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0 || path.IndexOf('\0') >= 0) return false;
            string[] segments = path.Split('/');
            foreach (string segment in segments)
                if (string.IsNullOrEmpty(segment) || segment == "." || segment == "..") return false;
            foreach (char c in path) if (char.IsControl(c)) return false;
            return true;
        }

        public static bool IsForbiddenWorkAsset(string path)
        {
            return ForbiddenWorkExtensions.Contains(Path.GetExtension(path)) || path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        public static string ManifestPath(string authorId, string workId)
        {
            if (!IsSafeId(authorId) || !IsSafeId(workId)) throw new ArgumentException("Author and work IDs must contain only letters, digits, '_' or '-'.");
            return ManifestRoot + StableManifestGuid(authorId, workId) + ".json";
        }

        public static string StableManifestGuid(string authorId, string workId)
        {
            using (var md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes("EchoLevelToolkit.Manifest.v1/" + authorId + "/" + workId));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public static DependencyScan Scan(string authorId, string workId, IEnumerable<string> roots)
        {
            var result = new DependencyScan();
            if (!IsSafeId(authorId) || !IsSafeId(workId))
            {
                result.errors.Add("Invalid author/work ID.");
                return result;
            }
            string workDirectory = WorkRoot + authorId + "/" + workId + "/";
            string[] rootPaths = (roots ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (rootPaths.Length == 0) result.errors.Add("Select at least one root asset.");
            foreach (string root in rootPaths)
                if (!IsSafeAssetPath(root) || !root.StartsWith(workDirectory, StringComparison.Ordinal)
                    || AssetDatabase.IsValidFolder(root) || AssetDatabase.LoadMainAssetAtPath(root) == null)
                    result.errors.Add("Root is missing or outside the work directory: " + root);
            if (result.errors.Count != 0) return result;

            foreach (string path in AssetDatabase.GetDependencies(rootPaths, true).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
            {
                var item = new DependencyItem { path = path };
                if (path.StartsWith(workDirectory, StringComparison.Ordinal))
                {
                    item.category = DependencyClass.WorkOwned;
                    if (!IsSafeAssetPath(path) || IsForbiddenWorkAsset(path)) result.errors.Add("Work asset is unsafe to exchange: " + path);
                }
                else if (path.StartsWith(ToolkitRoot, StringComparison.Ordinal))
                {
                    item.category = DependencyClass.ToolkitShared;
                    if (path.StartsWith(ManifestRoot, StringComparison.Ordinal)
                        || path.StartsWith("Assets/EchoLevelToolkit/Editor/Packaging/AcceptanceRecords/", StringComparison.Ordinal))
                        result.errors.Add("Work references packaging bookkeeping rather than public toolkit content: " + path);
                }
                else if (path.StartsWith("Packages/", StringComparison.Ordinal))
                {
                    item.category = DependencyClass.UnityOrUpm;
                    if (!path.StartsWith("Packages/com.unity.modules.", StringComparison.Ordinal))
                    {
                        UpmPackageInfo info = UpmPackageInfo.FindForAssetPath(path);
                        if (info == null || string.IsNullOrWhiteSpace(info.name) || string.IsNullOrWhiteSpace(info.version))
                            result.errors.Add("Cannot resolve UPM package/version: " + path);
                        else { item.packageId = info.name; item.packageVersion = info.version; }
                    }
                }
                else if (path.StartsWith("Resources/unity_builtin_extra", StringComparison.Ordinal)
                    || path.StartsWith("Library/unity default resources", StringComparison.Ordinal)) item.category = DependencyClass.UnityOrUpm;
                else
                {
                    item.category = DependencyClass.UndeclaredExternal;
                    result.errors.Add("Undeclared external dependency: " + path);
                }
                result.items.Add(item);
            }
            return result;
        }

        public static string ProjectAbsolutePath(string assetPath)
        {
            if (!IsSafeAssetPath(assetPath)) throw new ArgumentException("Unsafe asset path: " + assetPath);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        public static string Sha256(byte[] data)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }

        public static string Sha256File(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        public static PackageAsset SnapshotAsset(string assetPath)
        {
            string absolute = ProjectAbsolutePath(assetPath);
            if (!File.Exists(absolute) || !File.Exists(absolute + ".meta")) throw new IOException("Asset or .meta missing: " + assetPath);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) throw new IOException("GUID missing: " + assetPath);
            return new PackageAsset
            {
                path = assetPath, guid = guid.ToLowerInvariant(),
                assetSha256 = Sha256File(absolute), metaSha256 = Sha256File(absolute + ".meta")
            };
        }

        public static string Fingerprint(IEnumerable<PackageAsset> assets)
        {
            var data = new StringBuilder();
            foreach (PackageAsset a in assets.OrderBy(x => x.path, StringComparer.Ordinal))
                data.Append(a.path).Append('\n').Append(a.guid).Append('\n').Append(a.assetSha256).Append('\n').Append(a.metaSha256).Append('\n');
            return Sha256(Encoding.UTF8.GetBytes(data.ToString()));
        }

        public static string ContentFingerprint(IEnumerable<PackageAsset> owned, IEnumerable<PackageAsset> shared, string endpointManifestJson)
        {
            return Sha256(Encoding.UTF8.GetBytes(Fingerprint(owned.Concat(shared)) + "\n" + (endpointManifestJson ?? string.Empty)));
        }

        public static string VersionFingerprint(WorkPackageManifest manifest)
        {
            var data = new StringBuilder().Append(manifest.toolkitVersion).Append('\n').Append(manifest.contentFormatVersion)
                .Append('\n').Append(manifest.unityVersion).Append('\n').Append(manifest.urpVersion).Append('\n').Append(manifest.templateVersion).Append('\n');
            foreach (PackageRequirement p in manifest.packages.OrderBy(x => x.packageId, StringComparer.Ordinal))
                data.Append(p.packageId).Append('=').Append(p.exactVersion).Append('\n');
            return Sha256(Encoding.UTF8.GetBytes(data.ToString()));
        }
    }
}
