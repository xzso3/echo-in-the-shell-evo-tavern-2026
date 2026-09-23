using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Packaging;
using UnityEditor;
using UnityEngine;

namespace Echo.LevelToolkit.Editor.Packaging
{
    // KT-04/05 validators register here after their APIs settle. Formal exports require that hook.
    public static class PackageContentValidation
    {
        public static Func<WorkPackageKind, string[], IReadOnlyList<string>> ValidateContent;

        public static IReadOnlyList<string> Validate(WorkPackageKind kind, string[] roots, bool requireFullValidator)
        {
            var errors = new List<string>();
            if (roots == null || roots.Length == 0) { errors.Add("No roots selected."); return errors; }
            if (kind != WorkPackageKind.Level && roots.Length != 1) errors.Add("A single-item package needs one root asset.");
            foreach (string path in roots)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (kind == WorkPackageKind.Chunk && (!prefab || !prefab.GetComponent<ChunkDefinition>()
                    || !prefab.GetComponent<ChunkDefinition>().HasValidMetadata))
                    errors.Add("Chunk root must be a prefab with valid ChunkDefinition: " + path);
                if (kind == WorkPackageKind.Enemy && (!prefab || !prefab.GetComponent<CombatEnemy>()))
                    errors.Add("Enemy root must be a CombatEnemy prefab: " + path);
                if (kind == WorkPackageKind.Boss && (!prefab || !prefab.GetComponent<CombatBoss>()))
                    errors.Add("Boss root must be a CombatBoss prefab: " + path);
            }
            if (ValidateContent == null)
            {
                if (requireFullValidator) errors.Add("Full KT-04/05 content validator is not registered; formal export is disabled.");
            }
            else
            {
                IReadOnlyList<string> more = ValidateContent(kind, roots);
                if (more != null) errors.AddRange(more);
            }
            return errors;
        }
    }

    public static class WorkPackageExporter
    {
        public static WorkPackageManifest BuildManifest(string authorId, string workId, WorkPackageKind kind,
            string[] roots, bool internalCandidate)
        {
            DependencyScan scan = PackagePolicy.Scan(authorId, workId, roots);
            if (!scan.CanExport) throw new InvalidOperationException(string.Join("\n", scan.errors));
            string[] sortedRoots = roots.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            IReadOnlyList<string> validation = PackageContentValidation.Validate(kind, sortedRoots, !internalCandidate);
            if (validation.Count != 0) throw new InvalidOperationException(string.Join("\n", validation));
            if (kind == WorkPackageKind.Chunk)
            {
                ChunkDefinition chunk = AssetDatabase.LoadAssetAtPath<GameObject>(sortedRoots[0]).GetComponent<ChunkDefinition>();
                if (chunk.ContentId.AuthorId != authorId || chunk.ContentId.WorkId != workId)
                    throw new InvalidOperationException("Chunk ContentIdentity differs from work identity.");
            }
            var versionErrors = new List<string>();
            ToolkitVersion profile = PackagingVersion.FindProfile(versionErrors);
            if (profile == null) throw new InvalidOperationException(string.Join("\n", versionErrors));
            if (Application.unityVersion != profile.UnityVersion)
                throw new InvalidOperationException("Unity Editor version does not match ToolkitVersion: " + Application.unityVersion);

            var manifest = new WorkPackageManifest
            {
                authorId = authorId, workId = workId, kind = kind, rootAssets = sortedRoots,
                ownedAssets = scan.OwnedPaths.Select(PackagePolicy.SnapshotAsset).ToArray(),
                sharedAssets = scan.SharedPaths.Select(PackagePolicy.SnapshotAsset).ToArray(),
                usedPackages = scan.items.Where(x => x.category == DependencyClass.UnityOrUpm && x.packageId != null)
                    .GroupBy(x => x.packageId, StringComparer.Ordinal)
                    .Select(g => new PackageRequirement { packageId = g.Key, exactVersion = g.First().packageVersion })
                    .OrderBy(x => x.packageId, StringComparer.Ordinal).ToArray()
            };
            PackagingVersion.Fill(manifest, profile);
            if (kind == WorkPackageKind.Level)
            {
                LevelEndpointCatalog[] catalogs = scan.OwnedPaths.Select(AssetDatabase.LoadAssetAtPath<LevelEndpointCatalog>)
                    .Where(x => x != null).ToArray();
                if (catalogs.Length != 1) throw new InvalidOperationException("A Level package needs exactly one owned LevelEndpointCatalog.");
                if (catalogs[0].LevelIdentity.AuthorId != authorId || catalogs[0].LevelIdentity.WorkId != workId)
                    throw new InvalidOperationException("Endpoint catalog identity differs from work identity.");
                var catalogErrors = catalogs[0].ValidateCatalog();
                if (catalogErrors.Count != 0) throw new InvalidOperationException(string.Join("\n", catalogErrors));
                manifest.endpointManifestJson = catalogs[0].GenerateManifestJson();
            }
            var whitelist = manifest.packages.ToDictionary(x => x.packageId, x => x.exactVersion, StringComparer.Ordinal);
            foreach (PackageRequirement used in manifest.usedPackages)
                if (!whitelist.TryGetValue(used.packageId, out string exact) || exact != used.exactVersion)
                    throw new InvalidOperationException("UPM dependency is outside ToolkitVersion whitelist: " + used.packageId);
            manifest.contentFingerprint = PackagePolicy.ContentFingerprint(manifest.ownedAssets, manifest.sharedAssets, manifest.endpointManifestJson);
            string versionFingerprint = PackagePolicy.VersionFingerprint(manifest);
            if (internalCandidate)
                manifest.acceptance = new AcceptanceEvidence
                {
                    kind = AcceptanceKind.InternalCandidate, contentFingerprint = manifest.contentFingerprint,
                    versionFingerprint = versionFingerprint, note = "Internal S3 candidate; no human acceptance claimed."
                };
            else
            {
                string recordPath = AcceptancePath(authorId, workId);
                if (!File.Exists(PackagePolicy.ProjectAbsolutePath(recordPath)))
                    throw new InvalidOperationException("No human acceptance record for this work.");
                manifest.acceptance = JsonUtility.FromJson<AcceptanceEvidence>(File.ReadAllText(PackagePolicy.ProjectAbsolutePath(recordPath)));
                if (manifest.acceptance == null || !manifest.acceptance.Matches(manifest.contentFingerprint, versionFingerprint))
                    throw new InvalidOperationException("Human acceptance is stale; review the changed content/version again.");
            }
            return manifest;
        }

        public static string AcceptancePath(string authorId, string workId)
        {
            if (!PackagePolicy.IsSafeId(authorId) || !PackagePolicy.IsSafeId(workId)) throw new ArgumentException("Invalid author/work ID.");
            return "Assets/EchoLevelToolkit/Editor/Packaging/AcceptanceRecords/" + PackagePolicy.StableManifestGuid(authorId, workId) + ".json";
        }

        public static void RecordHumanAcceptance(string authorId, string workId, WorkPackageKind kind, string[] roots, string reviewer)
        {
            if (string.IsNullOrWhiteSpace(reviewer)) throw new ArgumentException("Reviewer is required.");
            WorkPackageManifest current = BuildManifest(authorId, workId, kind, roots, true);
            var evidence = new AcceptanceEvidence
            {
                kind = AcceptanceKind.HumanApproved, reviewer = reviewer.Trim(),
                acceptedAtUtc = DateTime.UtcNow.ToString("o"), contentFingerprint = current.contentFingerprint,
                versionFingerprint = PackagePolicy.VersionFingerprint(current),
                note = "Author explicitly confirmed visual, movement, and combat review in the editor."
            };
            string path = AcceptancePath(authorId, workId);
            Directory.CreateDirectory(Path.GetDirectoryName(PackagePolicy.ProjectAbsolutePath(path)));
            File.WriteAllText(PackagePolicy.ProjectAbsolutePath(path), JsonUtility.ToJson(evidence, true));
            AssetDatabase.ImportAsset(path);
        }

        public static WorkPackageManifest Export(string authorId, string workId, WorkPackageKind kind,
            string[] roots, bool internalCandidate, string outputPath)
        {
            WorkPackageManifest manifest = BuildManifest(authorId, workId, kind, roots, internalCandidate);
            string manifestPath = PackagePolicy.ManifestPath(authorId, workId);
            string absoluteManifest = PackagePolicy.ProjectAbsolutePath(manifestPath);
            string absoluteMeta = absoluteManifest + ".meta";
            bool hadManifest = File.Exists(absoluteManifest);
            if (hadManifest != File.Exists(absoluteMeta)) throw new IOException("Manifest asset/.meta pair is incomplete: " + manifestPath);
            if (hadManifest && !string.Equals(AssetDatabase.AssetPathToGUID(manifestPath),
                PackagePolicy.StableManifestGuid(authorId, workId), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Existing manifest GUID conflicts with work identity: " + manifestPath);
            byte[] previousManifest = hadManifest ? File.ReadAllBytes(absoluteManifest) : null;
            byte[] previousMeta = hadManifest ? File.ReadAllBytes(absoluteMeta) : null;
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteManifest));
            string stagingPath = outputPath + ".staging.unitypackage";
            if (File.Exists(stagingPath) || File.Exists(outputPath)) throw new IOException("Output or staging file already exists.");
            try
            {
                File.WriteAllText(absoluteManifest, JsonUtility.ToJson(manifest, true), new System.Text.UTF8Encoding(false));
                if (!hadManifest)
                    File.WriteAllText(absoluteMeta, "fileFormatVersion: 2\nguid: " + PackagePolicy.StableManifestGuid(authorId, workId)
                        + "\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n");
                AssetDatabase.ImportAsset(manifestPath, ImportAssetOptions.ForceSynchronousImport);
                string[] paths = manifest.ownedAssets.Select(x => x.path).Concat(new[] { manifestPath }).ToArray();
                AssetDatabase.ExportPackage(paths, stagingPath, ExportPackageOptions.Default);
                PackagePreflightResult check = PackagePreflight.Inspect(stagingPath);
                if (!check.CanImport) throw new InvalidDataException("Exported package failed preflight: " + string.Join("\n", check.errors));
                if (File.Exists(outputPath)) throw new IOException("Output already exists: " + outputPath);
                File.Move(stagingPath, outputPath);
                return manifest;
            }
            finally
            {
                if (File.Exists(stagingPath)) File.Delete(stagingPath);
                if (hadManifest)
                {
                    File.WriteAllBytes(absoluteManifest, previousManifest);
                    File.WriteAllBytes(absoluteMeta, previousMeta);
                    AssetDatabase.ImportAsset(manifestPath, ImportAssetOptions.ForceSynchronousImport);
                }
                else
                {
                    AssetDatabase.DeleteAsset(manifestPath);
                    if (File.Exists(absoluteManifest)) File.Delete(absoluteManifest);
                    if (File.Exists(absoluteMeta)) File.Delete(absoluteMeta);
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
