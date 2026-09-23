using System;
using System.Collections.Generic;
using System.Linq;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Packaging;
using UnityEditor;

namespace Echo.LevelToolkit.Editor.Packaging
{
    public static class PackagingVersion
    {
        public static ToolkitVersion FindProfile(List<string> errors)
        {
            string[] guids = AssetDatabase.FindAssets("t:ToolkitVersion", new[] { "Assets/EchoLevelToolkit" });
            if (guids.Length != 1) { errors.Add("Exactly one ToolkitVersion asset is required in Assets/EchoLevelToolkit."); return null; }
            ToolkitVersion profile = AssetDatabase.LoadAssetAtPath<ToolkitVersion>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (profile == null || !profile.IsComplete) { errors.Add("ToolkitVersion profile is incomplete."); return null; }
            if (!profile.Packages.Any(x => x.PackageId == "com.unity.render-pipelines.universal" && x.ExactVersion == profile.UrpVersion))
            { errors.Add("ToolkitVersion URP field must match the URP package whitelist."); return null; }
            return profile;
        }

        public static void Fill(WorkPackageManifest m, ToolkitVersion profile)
        {
            m.toolkitVersion = profile.Version;
            m.contentFormatVersion = profile.ContentFormatVersion;
            m.unityVersion = profile.UnityVersion;
            m.urpVersion = profile.UrpVersion;
            m.templateVersion = profile.TemplateVersion;
            m.packages = profile.Packages.Select(x => new PackageRequirement { packageId = x.PackageId, exactVersion = x.ExactVersion }).ToArray();
        }
    }
}
