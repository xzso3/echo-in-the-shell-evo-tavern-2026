using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echo.LevelToolkit.Foundation
{
    [Serializable]
    public struct PackageVersionRequirement
    {
        [SerializeField] private string packageId;
        [SerializeField] private string exactVersion;

        public string PackageId => packageId;
        public string ExactVersion => exactVersion;
        public bool IsComplete => !string.IsNullOrWhiteSpace(packageId)
            && !string.IsNullOrWhiteSpace(exactVersion);
    }

    // Version values are authored in a toolkit asset when the release is frozen.
    [CreateAssetMenu(menuName = "Echo/Level Toolkit/Toolkit Version")]
    public sealed class ToolkitVersion : ScriptableObject
    {
        [SerializeField] private string toolkitVersion;
        [SerializeField] private int contentFormatVersion;
        [SerializeField] private string unityVersion = "2021.3.27f1c2";
        [SerializeField] private string urpVersion = "12.1.12";
        [SerializeField] private string templateVersion;
        [SerializeField] private PackageVersionRequirement[] packages;

        public string Version => toolkitVersion;
        public int ContentFormatVersion => contentFormatVersion;
        public string UnityVersion => unityVersion;
        public string UrpVersion => urpVersion;
        public string TemplateVersion => templateVersion;
        public IReadOnlyList<PackageVersionRequirement> Packages => Array.AsReadOnly(packages ?? Array.Empty<PackageVersionRequirement>());

        public bool IsComplete => !string.IsNullOrWhiteSpace(toolkitVersion)
            && contentFormatVersion > 0 && !string.IsNullOrWhiteSpace(unityVersion)
            && !string.IsNullOrWhiteSpace(urpVersion) && !string.IsNullOrWhiteSpace(templateVersion)
            && HasCompletePackages();

        // The first release has no inferred compatibility across version changes.
        public bool MatchesExactly(ToolkitVersion other)
        {
            if (!IsComplete || !other || !other.IsComplete) return false;
            if (!string.Equals(toolkitVersion, other.toolkitVersion, StringComparison.Ordinal)
                || contentFormatVersion != other.contentFormatVersion
                || !string.Equals(unityVersion, other.unityVersion, StringComparison.Ordinal)
                || !string.Equals(urpVersion, other.urpVersion, StringComparison.Ordinal)
                || !string.Equals(templateVersion, other.templateVersion, StringComparison.Ordinal)
                || packages.Length != other.packages.Length) return false;

            var otherPackages = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var package in other.packages)
                otherPackages.Add(package.PackageId, package.ExactVersion);

            foreach (var package in packages)
            {
                if (!otherPackages.TryGetValue(package.PackageId, out var version)
                    || !string.Equals(package.ExactVersion, version, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private bool HasCompletePackages()
        {
            if (packages == null || packages.Length == 0) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var package in packages)
                if (!package.IsComplete || !ids.Add(package.PackageId)) return false;
            return true;
        }
    }
}
