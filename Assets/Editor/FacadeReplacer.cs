using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightGame.EditorTools
{
    /// <summary>
    /// One-shot refactor: replaces "facade" prefab instances (root has SimpleEditModeSprite
    /// wrapping a single nested prefab) with fresh instances of the wrapped prefab, preserving
    /// the wrapped child's world transform + local scale.
    ///
    /// Facade prefabs live in Assets/_Game/General/TilemapPrefabs/. Each wraps a "real" prefab
    /// (e.g. Ground2 wraps Ground.prefab with baked scale=2 and shadow-mesh bounds).
    /// After running, scenes reference the wrapped prefabs directly. Facade prefab assets are
    /// left in place — they may still be referenced by level-design brushes.
    /// </summary>
    public static class FacadeReplacer
    {
        // Map: facade prefab path -> wrapped prefab path (both relative to project root, forward-slash).
        // Sourced from the audit that produced the facade catalog. Only true facades are included
        // (single nested PrefabInstance child, SimpleEditModeSprite on root).
        private static readonly Dictionary<string, string> FacadeMap = new Dictionary<string, string>
        {
            { "Assets/_Game/General/TilemapPrefabs/Cube.prefab",              "Assets/_Game/Features/PushableObject/Prefabs/Cube.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/MovableCube.prefab",       "Assets/_Game/Features/PushableObject/Prefabs/MovableCube.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/HiddabelMovableCube.prefab","Assets/_Game/Features/PushableObject/Prefabs/HiddableMovableCube.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Ground2.prefab",           "Assets/_Game/Features/Environment/Prefabs/Ground.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Ground3.prefab",           "Assets/_Game/Features/Environment/Prefabs/Ground.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Ground6.prefab",           "Assets/_Game/Features/Environment/Prefabs/Ground.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Ground2Angle1.prefab",     "Assets/_Game/Features/Environment/Prefabs/Ground.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Ground3Angle1.prefab",     "Assets/_Game/Features/Environment/Prefabs/Ground.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Ground6Angle1.prefab",     "Assets/_Game/Features/Environment/Prefabs/Ground.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/HiddableGround3.prefab",   "Assets/_Game/Features/VFX/Prefabs/HidePlatforms.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Jumpad.prefab",            "Assets/_Game/Features/JumpPad/Prefabs/Jumpad.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Hint.prefab",              "Assets/_Game/Features/UI/Prefabs/Hints/Hint.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/LevelChangeLight.prefab",  "Assets/_Game/Features/Light/Prefabs/LevelChangeLight.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/LevelEntryLight.prefab",   "Assets/_Game/Features/Light/Prefabs/LevelEntryLight.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/MovingLight.prefab",       "Assets/_Game/Features/Light/Prefabs/MovingLight.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/MovingPlatform.prefab",    "Assets/_Game/Features/Environment/Prefabs/MovingPlatform.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/OneWayPlatform.prefab",    "Assets/_Game/Features/Environment/Prefabs/OneWayPlatform.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/ProjectileSpawner.prefab", "Assets/_Game/Features/Projectile/Prefabs/ProjectileSpawner.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/SlimeEnemy.prefab",        "Assets/_Game/Features/Enemy/Prefabs/SlimeEnemy.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/StationaryLight.prefab",   "Assets/_Game/Features/Light/Prefabs/StationaryLight.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Wall2.prefab",             "Assets/_Game/Features/Environment/Prefabs/Wall.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Wall3.prefab",             "Assets/_Game/Features/Environment/Prefabs/Wall.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Wall6.prefab",             "Assets/_Game/Features/Environment/Prefabs/Wall.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Wall2Angle1.prefab",       "Assets/_Game/Features/Environment/Prefabs/Wall.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Wall3Angle1.prefab",       "Assets/_Game/Features/Environment/Prefabs/Wall.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/Wall6Angle1.prefab",       "Assets/_Game/Features/Environment/Prefabs/Wall.prefab" },
            { "Assets/_Game/General/TilemapPrefabs/WeightPlatform.prefab",    "Assets/_Game/Features/Weight/Prefabs/WeightPlatform.prefab" },
        };

        [MenuItem("Tools/Refactor/Replace Facades In Current Scene")]
        public static void ReplaceInCurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[FacadeReplacer] No active scene.");
                return;
            }
            var report = new Report();
            ProcessScene(scene, report);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(report.ToString());
        }

        [MenuItem("Tools/Refactor/Replace Facades In All Scenes")]
        public static void ReplaceInAllScenes()
        {
            // Ensure the loaded map targets exist.
            var missing = FacadeMap
                .Where(kv => AssetDatabase.LoadAssetAtPath<GameObject>(kv.Value) == null || AssetDatabase.LoadAssetAtPath<GameObject>(kv.Key) == null)
                .Select(kv => $"  facade={kv.Key} wrapped={kv.Value}")
                .ToList();
            if (missing.Count > 0)
            {
                Debug.LogError("[FacadeReplacer] Missing prefab assets:\n" + string.Join("\n", missing));
                return;
            }

            // Save current scene state before we start swapping around.
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorSceneManager.SaveOpenScenes())
                {
                    Debug.LogError("[FacadeReplacer] Aborting: could not save open scenes.");
                    return;
                }
            }

            var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .ToList();

            var report = new Report();
            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var perSceneCounts = new Dictionary<string, int>();
                int replaced = ProcessScene(scene, report, perSceneCounts);
                if (replaced > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.SceneLog.AppendLine($"  {path}: {replaced} — {string.Join(", ", perSceneCounts.Select(kv => $"{kv.Key}={kv.Value}"))}");
                }
                else
                {
                    report.SceneLog.AppendLine($"  {path}: 0");
                }
            }

            Debug.Log(report.ToString());
        }

        private static int ProcessScene(Scene scene, Report report, Dictionary<string, int> perScene = null)
        {
            // Facade instances can be nested arbitrarily. Walk the whole scene, collect facade
            // roots first, then replace — replacing during traversal would invalidate iteration.
            var toReplace = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                CollectFacadeInstances(root.transform, toReplace);
            }

            int replaced = 0;
            foreach (var facadeGo in toReplace)
            {
                if (facadeGo == null) continue;
                string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(facadeGo);
                if (string.IsNullOrEmpty(sourcePath) || !FacadeMap.TryGetValue(sourcePath, out var wrappedPath)) continue;
                var wrappedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(wrappedPath);
                if (wrappedAsset == null)
                {
                    report.Errors.Add($"Missing wrapped prefab: {wrappedPath}");
                    continue;
                }

                if (facadeGo.transform.childCount == 0)
                {
                    report.Errors.Add($"Facade {sourcePath} in {scene.name} at {facadeGo.transform.position} has no child — skipping.");
                    continue;
                }

                Transform wrappedChild = facadeGo.transform.GetChild(0);
                Vector3 worldPos = wrappedChild.position;
                Quaternion worldRot = wrappedChild.rotation;
                Vector3 lossy = wrappedChild.lossyScale;
                Transform parent = facadeGo.transform.parent;
                int sibIndex = facadeGo.transform.GetSiblingIndex();
                string facadeName = facadeGo.name;

                var newInstance = (GameObject)PrefabUtility.InstantiatePrefab(wrappedAsset, scene);
                if (newInstance == null)
                {
                    report.Errors.Add($"Failed to instantiate {wrappedPath}");
                    continue;
                }
                newInstance.transform.SetParent(parent, worldPositionStays: false);
                newInstance.transform.SetSiblingIndex(sibIndex);
                newInstance.transform.SetPositionAndRotation(worldPos, worldRot);
                Vector3 parentLossy = parent != null ? parent.lossyScale : Vector3.one;
                newInstance.transform.localScale = new Vector3(
                    Approx(parentLossy.x, 0f) ? lossy.x : lossy.x / parentLossy.x,
                    Approx(parentLossy.y, 0f) ? lossy.y : lossy.y / parentLossy.y,
                    Approx(parentLossy.z, 0f) ? lossy.z : lossy.z / parentLossy.z);

                UnityEngine.Object.DestroyImmediate(facadeGo);
                replaced++;
                report.Totals[sourcePath] = report.Totals.GetValueOrDefault(sourcePath, 0) + 1;
                if (perScene != null)
                {
                    var key = Path.GetFileNameWithoutExtension(sourcePath);
                    perScene[key] = perScene.GetValueOrDefault(key, 0) + 1;
                }
            }
            return replaced;
        }

        private static void CollectFacadeInstances(Transform t, List<GameObject> collected)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
            {
                string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if (!string.IsNullOrEmpty(sourcePath) && FacadeMap.ContainsKey(sourcePath))
                {
                    collected.Add(t.gameObject);
                    return; // don't descend into the facade — its children will be discarded
                }
            }
            for (int i = 0; i < t.childCount; i++)
            {
                CollectFacadeInstances(t.GetChild(i), collected);
            }
        }

        private static bool Approx(float a, float b) => Mathf.Abs(a - b) < 0.00001f;

        private class Report
        {
            public readonly Dictionary<string, int> Totals = new Dictionary<string, int>();
            public readonly List<string> Errors = new List<string>();
            public readonly StringBuilder SceneLog = new StringBuilder();

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("[FacadeReplacer] Summary:");
                sb.AppendLine("Totals by facade:");
                foreach (var kv in Totals.OrderByDescending(k => k.Value))
                {
                    sb.AppendLine($"  {kv.Key} → {kv.Value}");
                }
                sb.AppendLine($"Grand total: {Totals.Values.Sum()}");
                if (Errors.Count > 0)
                {
                    sb.AppendLine("Errors:");
                    foreach (var e in Errors) sb.AppendLine("  " + e);
                }
                if (SceneLog.Length > 0)
                {
                    sb.AppendLine("Per-scene:");
                    sb.Append(SceneLog);
                }
                return sb.ToString();
            }
        }
    }
}
