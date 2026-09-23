using System;
using System.Linq;
using System.Text;
using Echo.LevelToolkit.Packaging;
using UnityEditor;
using UnityEngine;

namespace Echo.LevelToolkit.Editor.Packaging
{
    public sealed class WorkPackageWindow : EditorWindow
    {
        private string authorId = "author";
        private string workId = "work";
        private string reviewer = "";
        private WorkPackageKind kind;
        private Vector2 scroll;
        private string report = "Select work root assets in the Project window.";

        [MenuItem("Echo/Level Toolkit/Export Work Package")]
        private static void ShowWindow() => GetWindow<WorkPackageWindow>("Work Package");

        private void OnGUI()
        {
            authorId = EditorGUILayout.TextField("Author ID", authorId);
            workId = EditorGUILayout.TextField("Work ID", workId);
            kind = (WorkPackageKind)EditorGUILayout.EnumPopup("Package kind", kind);
            reviewer = EditorGUILayout.TextField("Human reviewer", reviewer);
            EditorGUILayout.HelpBox("Select one Chunk, Enemy, or Boss prefab; for a Level select every root asset needed for the work. Roots must live under Assets/EchoUserContent/<author>/<work>/.", MessageType.Info);
            if (GUILayout.Button("Scan dependencies and validate")) Run(() =>
            {
                string[] roots = SelectedRoots();
                DependencyScan scan = PackagePolicy.Scan(authorId, workId, roots);
                var output = new StringBuilder();
                foreach (DependencyItem item in scan.items) output.AppendLine(item.category + "  " + item.path);
                foreach (string error in scan.errors) output.AppendLine("ERROR: " + error);
                foreach (string error in PackageContentValidation.Validate(kind, roots, false)) output.AppendLine("ERROR: " + error);
                report = output.ToString();
            });
            if (GUILayout.Button("Record human acceptance for current fingerprint")) Run(() =>
            {
                if (!EditorUtility.DisplayDialog("Confirm personal review",
                    "Confirm that you personally played/reviewed the current visuals, movement, combat and composition. This record expires whenever content, shared dependencies or toolkit version changes.",
                    "I personally reviewed", "Cancel")) return;
                WorkPackageExporter.RecordHumanAcceptance(authorId, workId, kind, SelectedRoots(), reviewer);
                report = "Human acceptance recorded for the current content and version fingerprint.";
            });
            if (GUILayout.Button("Export formally accepted work")) Export(false);
            if (GUILayout.Button("Export internal S3 candidate (unaccepted)")) Export(true);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Export(bool candidate)
        {
            Run(() =>
            {
                string filename = authorId + "-" + workId + (candidate ? "-candidate" : "") + ".unitypackage";
                string output = EditorUtility.SaveFilePanel("Export work package", "", filename, "unitypackage");
                if (string.IsNullOrEmpty(output)) return;
                WorkPackageManifest manifest = WorkPackageExporter.Export(authorId, workId, kind, SelectedRoots(), candidate, output);
                report = "Exported: " + output + "\nFingerprint: " + manifest.contentFingerprint
                    + "\nAcceptance: " + manifest.acceptance.kind;
            });
        }

        private static string[] SelectedRoots() => Selection.objects.Select(AssetDatabase.GetAssetPath)
            .Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).ToArray();
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { report = "ERROR: " + ex.Message; Debug.LogException(ex); }
        }
    }

    public sealed class ReceiveWorkWindow : EditorWindow
    {
        private string packagePath;
        private PackagePreflightResult result;
        private Vector2 scroll;

        [MenuItem("Echo/Level Toolkit/Receive Work Package")]
        private static void ShowWindow() => GetWindow<ReceiveWorkWindow>("Receive Work");

        private void OnGUI()
        {
            if (GUILayout.Button("Choose .unitypackage and preflight"))
            {
                packagePath = EditorUtility.OpenFilePanel("Choose work package", "", "unitypackage");
                result = string.IsNullOrEmpty(packagePath) ? null : PackagePreflight.Inspect(packagePath);
            }
            if (result == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField(packagePath, EditorStyles.wordWrappedLabel);
            if (result.manifest != null)
            {
                EditorGUILayout.LabelField("Work", result.manifest.WorkKey);
                EditorGUILayout.LabelField("Kind", result.manifest.kind.ToString());
                EditorGUILayout.LabelField("Acceptance", result.manifest.acceptance?.kind.ToString() ?? "Missing");
                EditorGUILayout.LabelField("Owned assets", result.manifest.ownedAssets?.Length.ToString() ?? "0");
            }
            foreach (string error in result.errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            foreach (string change in result.changes) EditorGUILayout.LabelField("Update: " + change);
            EditorGUILayout.EndScrollView();
            EditorGUI.BeginDisabledGroup(!result.CanImport);
            if (GUILayout.Button("Import after preflight"))
            {
                // Recheck immediately before mutation; no package is imported from a stale report.
                result = PackagePreflight.Inspect(packagePath);
                if (!result.CanImport) return;
                if (result.requiresUpdateConsent && !EditorUtility.DisplayDialog("Accept existing work update",
                    "This package updates " + result.manifest.WorkKey + ". Changed assets:\n"
                    + string.Join("\n", result.changes), "Accept update", "Cancel")) return;
                if (result.manifest.acceptance.kind == AcceptanceKind.InternalCandidate
                    && !EditorUtility.DisplayDialog("Internal candidate package",
                        "This package has no formal human acceptance. Import only for S3/testing; it is not approved for formal use.",
                        "Import candidate", "Cancel")) return;
                WorkPackageReceiver.ImportValidated(packagePath, result.requiresUpdateConsent,
                    result.manifest.acceptance.kind == AcceptanceKind.InternalCandidate);
                result = null;
                // Intentionally no scene open, Play, map insertion or narrative binding here.
            }
            EditorGUI.EndDisabledGroup();
        }
    }

}
