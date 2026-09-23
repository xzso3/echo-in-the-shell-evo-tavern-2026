using System;
using UnityEditor;

namespace Echo.LevelToolkit.Editor.Packaging
{
    public static class WorkPackageReceiver
    {
        // Batch and UI call the same entry. Native Unity import UI is intentionally bypassed.
        public static PackagePreflightResult ImportValidated(string packagePath, bool acceptExistingWorkUpdate,
            bool acceptInternalCandidate)
        {
            PackagePreflightResult result = PackagePreflight.Inspect(packagePath);
            if (!result.CanImport) throw new InvalidOperationException("Preflight failed: " + string.Join("\n", result.errors));
            if (result.requiresUpdateConsent && !acceptExistingWorkUpdate)
                throw new InvalidOperationException("Existing work update needs explicit acceptance.");
            if (result.manifest.acceptance.kind == Echo.LevelToolkit.Packaging.AcceptanceKind.InternalCandidate
                && !acceptInternalCandidate)
                throw new InvalidOperationException("Internal candidate needs explicit acceptance for testing import.");
            AssetDatabase.ImportPackage(packagePath, false);
            return result;
        }
    }
}
