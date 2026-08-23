#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LightGame.Editor
{
    /// <summary>
    /// Keeps Build Settings in sync with scenes under the Levels folder, so adding/renaming/
    /// removing a level scene never requires a manual Build Settings edit.
    /// </summary>
    public class LevelScenePostprocessor : AssetPostprocessor
    {
        private const string LevelsFolder = "Assets/_Game/Scenes/Levels/";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool changed = false;

            foreach (var path in importedAssets)
            {
                if (IsLevelScene(path))
                    changed |= AddToBuildSettings(path);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                var newPath = movedAssets[i];
                var oldPath = movedFromAssetPaths[i];
                if (IsLevelScene(oldPath) || IsLevelScene(newPath))
                    changed |= UpdateBuildSettingsPath(oldPath, newPath);
            }

            foreach (var path in deletedAssets)
            {
                if (IsLevelScene(path))
                    changed |= RemoveFromBuildSettings(path);
            }

            if (changed)
                Debug.Log("Level scenes: Build Settings updated automatically.");
        }

        // OnPostprocessAllAssets only fires for import events Unity actually observes live,
        // which it skips while the project has compile errors and never replays afterward.
        // Reconcile against the real state of the Levels folder on every reload to catch drift.
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void ReconcileOnReload()
        {
            bool changed = false;

            var scenesOnDisk = AssetDatabase.FindAssets("t:Scene", new[] { LevelsFolder.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsLevelScene)
                .ToList();

            foreach (var path in scenesOnDisk)
                changed |= AddToBuildSettings(path);

            var registeredLevelScenes = EditorBuildSettings.scenes
                .Where(s => IsLevelScene(s.path))
                .Select(s => s.path)
                .ToList();

            foreach (var path in registeredLevelScenes)
            {
                if (!scenesOnDisk.Contains(path))
                    changed |= RemoveFromBuildSettings(path);
            }

            if (changed)
                Debug.Log("Level scenes: Build Settings reconciled after script reload.");
        }

        private static bool IsLevelScene(string path) =>
            path.StartsWith(LevelsFolder) && path.EndsWith(".unity");

        private static bool AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return false;

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Added '{scenePath}' to Build Settings");
            return true;
        }

        private static bool RemoveFromBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            int removed = scenes.RemoveAll(s => s.path == scenePath);
            if (removed == 0) return false;

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Removed '{scenePath}' from Build Settings");
            return true;
        }

        private static bool UpdateBuildSettingsPath(string oldPath, string newPath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(s => s.path == oldPath);
            if (index == -1)
                return IsLevelScene(newPath) && AddToBuildSettings(newPath);

            scenes[index] = new EditorBuildSettingsScene(newPath, scenes[index].enabled);
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Updated Build Settings path: '{oldPath}' -> '{newPath}'");
            return true;
        }
    }
}
#endif
