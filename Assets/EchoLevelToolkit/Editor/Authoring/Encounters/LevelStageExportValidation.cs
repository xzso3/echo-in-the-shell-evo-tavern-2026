using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.LevelToolkit.Level.Editor
{
    // KT-08 may call this from its WorkPackageKind.FullLevel ValidateContent hook.
    // It has no dependency on Packaging, so single-resource exports can skip it.
    public static class LevelStageExportValidation
    {
        public static IReadOnlyList<string> ValidateContentPaths(string[] assetPaths)
        {
            var errors = new List<string>();
            if (assetPaths == null || assetPaths.Length == 0)
            { errors.Add("KT05_LEVEL_MISSING: No level asset paths supplied."); return errors; }
            int found = 0;
            foreach (string path in assetPaths)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!prefab) continue;
                    foreach (LevelStage stage in prefab.GetComponentsInChildren<LevelStage>(true))
                    { found++; AddErrors(path, stage, errors); }
                }
                else if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    Scene scene = SceneManager.GetSceneByPath(path);
                    bool opened = !scene.IsValid();
                    Scene previous = SceneManager.GetActiveScene();
                    try
                    {
                        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        if (scene.isDirty)
                            errors.Add("KT05_SCENE_DIRTY: Save the scene before export: " + path);
                        foreach (GameObject root in scene.GetRootGameObjects())
                            foreach (LevelStage stage in root.GetComponentsInChildren<LevelStage>(true))
                            { found++; AddErrors(path, stage, errors); }
                    }
                    catch (Exception exception)
                    { errors.Add("KT05_SCENE_READ: " + path + ": " + exception.Message); }
                    finally
                    {
                        if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
                        if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                    }
                }
            }
            if (found == 0) errors.Add("KT05_LEVEL_MISSING: No LevelStage in the full-level asset paths.");
            return errors;
        }

        private static void AddErrors(string path, LevelStage stage, List<string> errors)
        {
            foreach (string error in LevelStageAuthoring.Validate(stage))
                errors.Add(error + " [" + path + "]");
        }
    }
}
