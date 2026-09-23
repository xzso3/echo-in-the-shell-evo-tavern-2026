using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Packaging;
using UnityEditor;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace Echo.LevelToolkit.Editor.Packaging
{
    public sealed class PackagePreflightResult
    {
        public WorkPackageManifest manifest;
        public readonly List<string> errors = new List<string>();
        public readonly List<string> changes = new List<string>();
        public bool requiresUpdateConsent;
        public bool CanImport => errors.Count == 0;
    }

    public static class PackagePreflight
    {
        // Callable from UI and batch integrations. No target AssetDatabase mutation occurs here.
        public static PackagePreflightResult Inspect(string packagePath)
        {
            var result = new PackagePreflightResult();
            try { InspectCore(packagePath, result); }
            catch (Exception ex)
            { result.errors.Add("Invalid package: " + ex.Message); }
            return result;
        }

        private static void InspectCore(string packagePath, PackagePreflightResult result)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
                throw new FileNotFoundException("Package does not exist.", packagePath);
            Dictionary<string, UnityPackageArchive.Asset> archive = UnityPackageArchive.Read(packagePath);
            var manifestCandidates = archive.Values.Where(x => !x.isFolder && x.path.StartsWith(PackagePolicy.ManifestRoot, StringComparison.Ordinal)
                && x.path.EndsWith(".json", StringComparison.Ordinal)).ToArray();
            if (manifestCandidates.Length != 1) { result.errors.Add("Exactly one work manifest is required."); return; }
            UnityPackageArchive.Asset manifestAsset = manifestCandidates[0];
            if (manifestAsset.content.smallContent == null) { result.errors.Add("Manifest exceeds 2 MB."); return; }
            WorkPackageManifest manifest = JsonUtility.FromJson<WorkPackageManifest>(Encoding.UTF8.GetString(manifestAsset.content.smallContent));
            result.manifest = manifest;
            if (manifest == null || manifest.schemaVersion != 1 || !PackagePolicy.IsSafeId(manifest.authorId)
                || !PackagePolicy.IsSafeId(manifest.workId) || !Enum.IsDefined(typeof(WorkPackageKind), manifest.kind))
            { result.errors.Add("Manifest schema, identity, or kind is invalid."); return; }
            string expectedManifestPath = PackagePolicy.ManifestPath(manifest.authorId, manifest.workId);
            string expectedManifestGuid = PackagePolicy.StableManifestGuid(manifest.authorId, manifest.workId);
            if (manifestAsset.path != expectedManifestPath || !string.Equals(manifestAsset.guid, expectedManifestGuid, StringComparison.OrdinalIgnoreCase))
                result.errors.Add("Manifest path/GUID does not match stable work identity.");

            ValidateManifestShape(manifest, archive, result);
            ValidateVersions(manifest, result);
            ValidateExisting(manifest, archive, result);
        }

        private static void ValidateManifestShape(WorkPackageManifest m, Dictionary<string, UnityPackageArchive.Asset> archive, PackagePreflightResult result)
        {
            var owned = m.ownedAssets ?? Array.Empty<PackageAsset>();
            var shared = m.sharedAssets ?? Array.Empty<PackageAsset>();
            string root = m.WorkDirectory;
            var ownedPaths = new HashSet<string>(StringComparer.Ordinal);
            var ownedGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var archivePaths = new HashSet<string>(archive.Values.Where(x => !x.isFolder).Select(x => x.path), StringComparer.Ordinal);
            if (owned.Length == 0) result.errors.Add("Manifest lists no work assets.");
            foreach (PackageAsset asset in owned)
            {
                if (asset == null || !PackagePolicy.IsSafeAssetPath(asset.path)
                    || !asset.path.StartsWith(root, StringComparison.Ordinal)
                    || PackagePolicy.IsForbiddenWorkAsset(asset.path)
                    || !IsDigest(asset.assetSha256) || !IsDigest(asset.metaSha256)
                    || !IsGuid(asset.guid) || !ownedPaths.Add(asset.path) || !ownedGuids.Add(asset.guid))
                { result.errors.Add("Invalid or duplicate owned asset record: " + asset?.path); continue; }
                UnityPackageArchive.Asset entry;
                if (!archive.TryGetValue(asset.guid, out entry) || entry.path != asset.path
                    || entry.content.sha256 != asset.assetSha256 || entry.meta.sha256 != asset.metaSha256)
                    result.errors.Add("Archive content differs from manifest: " + asset.path);
            }
            var expectedPaths = new HashSet<string>(ownedPaths, StringComparer.Ordinal) { PackagePolicy.ManifestPath(m.authorId, m.workId) };
            if (!expectedPaths.SetEquals(archivePaths)) result.errors.Add("Archive includes missing or undeclared assets.");
            foreach (UnityPackageArchive.Asset folder in archive.Values.Where(x => x.isFolder))
                if (!expectedPaths.Any(x => x.StartsWith(folder.path + "/", StringComparison.Ordinal)))
                    result.errors.Add("Archive includes unrelated folder: " + folder.path);
            if (m.rootAssets == null || m.rootAssets.Length == 0 || m.rootAssets.Any(x => !ownedPaths.Contains(x)))
                result.errors.Add("Roots are missing from owned asset records.");
            if (m.kind != WorkPackageKind.Level && m.rootAssets != null && m.rootAssets.Length != 1)
                result.errors.Add("A single-item package must have exactly one root.");
            var sharedPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (PackageAsset asset in shared)
            {
                if (asset == null || !PackagePolicy.IsSafeAssetPath(asset.path)
                    || !asset.path.StartsWith(PackagePolicy.ToolkitRoot, StringComparison.Ordinal)
                    || asset.path.StartsWith(PackagePolicy.ManifestRoot, StringComparison.Ordinal)
                    || !IsGuid(asset.guid) || !IsDigest(asset.assetSha256) || !IsDigest(asset.metaSha256)
                    || !sharedPaths.Add(asset.path) || archivePaths.Contains(asset.path))
                    result.errors.Add("Invalid shared dependency declaration: " + asset?.path);
            }
            ValidateSerializedReferences(m, archive, result);
            if (m.kind == WorkPackageKind.Level && string.IsNullOrWhiteSpace(m.endpointManifestJson))
                result.errors.Add("Level endpoint manifest is missing.");
            if (PackagePolicy.ContentFingerprint(owned, shared, m.endpointManifestJson) != m.contentFingerprint)
                result.errors.Add("Content fingerprint differs from asset records.");
            string versionFingerprint = PackagePolicy.VersionFingerprint(m);
            if (m.acceptance == null || (m.acceptance.kind == AcceptanceKind.HumanApproved
                && !m.acceptance.Matches(m.contentFingerprint, versionFingerprint)))
                result.errors.Add("Human acceptance is absent or stale.");
            else if (m.acceptance.kind == AcceptanceKind.InternalCandidate
                && (m.acceptance.contentFingerprint != m.contentFingerprint
                    || m.acceptance.versionFingerprint != versionFingerprint
                    || string.IsNullOrWhiteSpace(m.acceptance.note)))
                result.errors.Add("Internal candidate evidence is stale or incomplete.");
            else if (m.acceptance.kind != AcceptanceKind.HumanApproved && m.acceptance.kind != AcceptanceKind.InternalCandidate)
                result.errors.Add("Acceptance status is invalid.");
        }

        private static void ValidateSerializedReferences(WorkPackageManifest m,
            Dictionary<string, UnityPackageArchive.Asset> archive, PackagePreflightResult result)
        {
            var allowed = new HashSet<string>((m.ownedAssets ?? Array.Empty<PackageAsset>()).Where(x => x != null).Select(x => x.guid), StringComparer.OrdinalIgnoreCase);
            allowed.UnionWith((m.sharedAssets ?? Array.Empty<PackageAsset>()).Where(x => x != null).Select(x => x.guid));
            allowed.Add(PackagePolicy.StableManifestGuid(m.authorId, m.workId));
            var used = new HashSet<string>((m.usedPackages ?? Array.Empty<PackageRequirement>()).Where(x => x != null).Select(x => x.packageId), StringComparer.Ordinal);
            foreach (UnityPackageArchive.Asset asset in archive.Values.Where(x => !x.isFolder))
            {
                foreach (string guid in asset.content.referenceGuids.Concat(asset.meta.referenceGuids).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (allowed.Contains(guid) || IsUnityBuiltinGuid(guid)) continue;
                    string resolved = AssetDatabase.GUIDToAssetPath(guid);
                    if (resolved.StartsWith("Packages/", StringComparison.Ordinal))
                    {
                        UpmPackageInfo info = UpmPackageInfo.FindForAssetPath(resolved);
                        if (info != null && used.Contains(info.name)) continue;
                    }
                    if (resolved.StartsWith("Resources/unity_builtin_extra", StringComparison.Ordinal)
                        || resolved.StartsWith("Library/unity default resources", StringComparison.Ordinal)) continue;
                    result.errors.Add("Undeclared serialized GUID reference " + guid + " in " + asset.path);
                }
            }
        }

        private static void ValidateVersions(WorkPackageManifest m, PackagePreflightResult result)
        {
            ToolkitVersion local = PackagingVersion.FindProfile(result.errors);
            if (local == null) return;
            if (m.toolkitVersion != local.Version || m.contentFormatVersion != local.ContentFormatVersion
                || m.unityVersion != local.UnityVersion || m.urpVersion != local.UrpVersion
                || m.templateVersion != local.TemplateVersion)
                result.errors.Add("Toolkit/content/Unity/URP/template version mismatch.");
            if (Application.unityVersion != m.unityVersion) result.errors.Add("Unity Editor version mismatch: " + Application.unityVersion);
            var declared = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (PackageRequirement p in m.packages ?? Array.Empty<PackageRequirement>())
            {
                if (p == null || string.IsNullOrWhiteSpace(p.packageId) || string.IsNullOrWhiteSpace(p.exactVersion)
                    || declared.ContainsKey(p.packageId)) { result.errors.Add("Invalid package whitelist."); continue; }
                declared.Add(p.packageId, p.exactVersion);
            }
            var expected = local.Packages.ToDictionary(x => x.PackageId, x => x.ExactVersion, StringComparer.Ordinal);
            if (declared.Count != expected.Count || expected.Any(x => !declared.TryGetValue(x.Key, out string v) || v != x.Value))
                result.errors.Add("Package whitelist does not match installed toolkit profile.");
            var installed = UpmPackageInfo.GetAllRegisteredPackages().ToDictionary(x => x.name, x => x.version, StringComparer.Ordinal);
            foreach (var requirement in declared)
                if (!installed.TryGetValue(requirement.Key, out string version) || version != requirement.Value)
                    result.errors.Add("UPM version missing/mismatched: " + requirement.Key + "@" + requirement.Value);
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (PackageRequirement p in m.usedPackages ?? Array.Empty<PackageRequirement>())
            {
                if (p == null || string.IsNullOrWhiteSpace(p.packageId) || !usedIds.Add(p.packageId))
                { result.errors.Add("Duplicate or invalid used UPM package."); continue; }
                if (p == null || !declared.TryGetValue(p.packageId ?? "", out string version) || version != p.exactVersion)
                    result.errors.Add("Used UPM dependency is outside version whitelist: " + p?.packageId);
            }
        }

        private static void ValidateExisting(WorkPackageManifest m, Dictionary<string, UnityPackageArchive.Asset> archive, PackagePreflightResult result)
        {
            foreach (PackageAsset shared in m.sharedAssets ?? Array.Empty<PackageAsset>())
            {
                if (shared == null || !PackagePolicy.IsSafeAssetPath(shared.path)) continue;
                string existingPath = AssetDatabase.GUIDToAssetPath(shared.guid ?? "");
                if (existingPath != shared.path || !string.Equals(AssetDatabase.AssetPathToGUID(shared.path), shared.guid, StringComparison.OrdinalIgnoreCase)
                    || !File.Exists(PackagePolicy.ProjectAbsolutePath(shared.path))
                    || PackagePolicy.Sha256File(PackagePolicy.ProjectAbsolutePath(shared.path)) != shared.assetSha256
                    || PackagePolicy.Sha256File(PackagePolicy.ProjectAbsolutePath(shared.path) + ".meta") != shared.metaSha256)
                    result.errors.Add("Toolkit shared dependency missing/modified: " + shared.path);
            }
            string manifestPath = PackagePolicy.ManifestPath(m.authorId, m.workId);
            bool isUpdate = File.Exists(PackagePolicy.ProjectAbsolutePath(manifestPath));
            if (isUpdate)
            {
                try
                {
                    WorkPackageManifest previous = JsonUtility.FromJson<WorkPackageManifest>(File.ReadAllText(PackagePolicy.ProjectAbsolutePath(manifestPath)));
                    if (previous == null || previous.WorkKey != m.WorkKey) result.errors.Add("Existing work manifest has another identity.");
                    else result.requiresUpdateConsent = true;
                }
                catch (Exception) { result.errors.Add("Existing work manifest cannot be read."); }
            }
            foreach (UnityPackageArchive.Asset asset in archive.Values)
            {
                string localByGuid = AssetDatabase.GUIDToAssetPath(asset.guid);
                string localGuidAtPath = AssetDatabase.AssetPathToGUID(asset.path);
                if (!string.IsNullOrEmpty(localByGuid) && localByGuid != asset.path)
                    result.errors.Add("Same GUID belongs to another resource: " + asset.guid + " -> " + localByGuid);
                if (!string.IsNullOrEmpty(localGuidAtPath) && !string.Equals(localGuidAtPath, asset.guid, StringComparison.OrdinalIgnoreCase))
                    result.errors.Add("Same path has another GUID: " + asset.path);
                if (asset.isFolder)
                {
                    string absoluteFolder = PackagePolicy.ProjectAbsolutePath(asset.path);
                    if (Directory.Exists(absoluteFolder) && File.Exists(absoluteFolder + ".meta")
                        && PackagePolicy.Sha256File(absoluteFolder + ".meta") != asset.meta.sha256)
                        result.errors.Add("Folder metadata would overwrite existing folder: " + asset.path);
                    continue;
                }
                if (File.Exists(PackagePolicy.ProjectAbsolutePath(asset.path)))
                {
                    if (!isUpdate) result.errors.Add("Existing asset path belongs to no accepted update: " + asset.path);
                    else if (PackagePolicy.Sha256File(PackagePolicy.ProjectAbsolutePath(asset.path)) != asset.content.sha256
                        || PackagePolicy.Sha256File(PackagePolicy.ProjectAbsolutePath(asset.path) + ".meta") != asset.meta.sha256)
                        result.changes.Add(asset.path);
                }
            }
        }

        private static bool IsGuid(string value) => value != null && value.Length == 32 && value.All(Uri.IsHexDigit);
        private static bool IsDigest(string value) => value != null && value.Length == 64 && value.All(Uri.IsHexDigit);
        private static bool IsUnityBuiltinGuid(string guid) => guid == "00000000000000000000000000000000"
            || guid == "0000000000000000d000000000000000"
            || guid == "0000000000000000e000000000000000"
            || guid == "0000000000000000f000000000000000";
    }

}
