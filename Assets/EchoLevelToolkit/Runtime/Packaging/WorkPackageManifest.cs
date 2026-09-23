using System;

namespace Echo.LevelToolkit.Packaging
{
    public enum WorkPackageKind { Chunk, Enemy, Boss, Level }
    public enum AcceptanceKind { None, HumanApproved, InternalCandidate }

    // JsonUtility-compatible transfer schema. All paths are project-relative Unity asset paths.
    [Serializable]
    public sealed class WorkPackageManifest
    {
        public int schemaVersion = 1;
        public string authorId;
        public string workId;
        public WorkPackageKind kind;
        public string[] rootAssets = Array.Empty<string>();
        public string toolkitVersion;
        public int contentFormatVersion;
        public string unityVersion;
        public string urpVersion;
        public string templateVersion;
        public PackageRequirement[] packages = Array.Empty<PackageRequirement>();
        public PackageAsset[] ownedAssets = Array.Empty<PackageAsset>();
        public PackageAsset[] sharedAssets = Array.Empty<PackageAsset>();
        public PackageRequirement[] usedPackages = Array.Empty<PackageRequirement>();
        public string endpointManifestJson;
        public string contentFingerprint;
        public AcceptanceEvidence acceptance = new AcceptanceEvidence();

        public string WorkKey => (authorId ?? string.Empty) + "/" + (workId ?? string.Empty);
        public string WorkDirectory => "Assets/EchoUserContent/" + authorId + "/" + workId + "/";
    }

    [Serializable]
    public sealed class PackageAsset
    {
        public string path;
        public string guid;
        public string assetSha256;
        public string metaSha256;
    }

    [Serializable]
    public sealed class PackageRequirement
    {
        public string packageId;
        public string exactVersion;
    }

    [Serializable]
    public sealed class AcceptanceEvidence
    {
        public AcceptanceKind kind;
        public string reviewer;
        public string acceptedAtUtc;
        public string contentFingerprint;
        public string versionFingerprint;
        public string note;

        public bool Matches(string content, string version)
        {
            return kind == AcceptanceKind.HumanApproved
                && !string.IsNullOrWhiteSpace(reviewer)
                && !string.IsNullOrWhiteSpace(acceptedAtUtc)
                && string.Equals(contentFingerprint, content, StringComparison.Ordinal)
                && string.Equals(versionFingerprint, version, StringComparison.Ordinal);
        }
    }
}
