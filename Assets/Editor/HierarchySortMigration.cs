using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LightGame.EditorTools
{
    // Drives 2D draw order from hierarchy sibling index instead of hand-tuned sortingOrder.
    // Adds a SortingGroup to every parent with 2+ SpriteRenderer descendants (isolating each
    // subtree from cross-branch bleed), then assigns each SortingGroup and each SpriteRenderer
    // a sortingOrder equal to its sibling index. Reparenting in the hierarchy now dictates
    // draw order — no more manual sortingOrder tuning.
    //
    // Non-Default sorting layers (e.g. LightSource for URP 2D light routing) are respected:
    // subtrees whose descendants use a non-Default layer are skipped so the layer is not
    // stolen by a wrapping SortingGroup on Default.
    public static class HierarchySortMigration
    {
        [MenuItem("Tools/Hierarchy Sort/Migrate Active Scene")]
        public static void MigrateActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            int addedGroups = 0, touchedRenderers = 0, touchedGroups = 0, skippedSubtrees = 0;
            var defaultLayerId = SortingLayer.NameToID("Default");

            foreach (var root in scene.GetRootGameObjects())
                Walk(root.transform, defaultLayerId, ref addedGroups, ref touchedRenderers, ref touchedGroups, ref skippedSubtrees);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[HierarchySortMigration] added {addedGroups} SortingGroups; " +
                      $"assigned sortingOrder on {touchedRenderers} SpriteRenderers, {touchedGroups} SortingGroups; " +
                      $"{skippedSubtrees} subtrees skipped (non-Default sortingLayer descendants).");
        }

        [MenuItem("Tools/Hierarchy Sort/Sync Now")]
        public static void SyncNow()
        {
            var scene = SceneManager.GetActiveScene();
            int r = 0, g = 0;
            foreach (var root in scene.GetRootGameObjects())
                SyncOrder(root.transform, ref r, ref g);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[HierarchySortMigration] Sync updated {r} SpriteRenderers, {g} SortingGroups.");
        }

        [MenuItem("Tools/Hierarchy Sort/Revert (remove added SortingGroups, zero sortingOrder)")]
        public static void RevertMigration()
        {
            var scene = SceneManager.GetActiveScene();
            int removed = 0, zeroed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var sg in root.GetComponentsInChildren<SortingGroup>(true))
                {
                    Object.DestroyImmediate(sg);
                    removed++;
                }
                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sortingOrder != 0) { sr.sortingOrder = 0; zeroed++; }
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[HierarchySortMigration] removed {removed} SortingGroups; zeroed sortingOrder on {zeroed} renderers.");
        }

        static void Walk(Transform t, int defaultLayerId,
            ref int addedGroups, ref int touchedRenderers, ref int touchedGroups, ref int skippedSubtrees)
        {
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                int idx = t.GetSiblingIndex();
                if (sr.sortingOrder != idx) sr.sortingOrder = idx;
                touchedRenderers++;
            }

            int spriteDescendants = 0;
            bool hasNonDefaultLayerDescendant = false;
            foreach (var s in t.GetComponentsInChildren<SpriteRenderer>(true))
            {
                spriteDescendants++;
                if (s.sortingLayerID != defaultLayerId) hasNonDefaultLayerDescendant = true;
            }

            var group = t.GetComponent<SortingGroup>();

            bool shouldGroup = spriteDescendants >= 2 && !hasNonDefaultLayerDescendant;
            if (shouldGroup && group == null)
            {
                group = t.gameObject.AddComponent<SortingGroup>();
                addedGroups++;
            }
            if (group != null)
            {
                int idx = t.GetSiblingIndex();
                if (group.sortingOrder != idx) group.sortingOrder = idx;
                touchedGroups++;
            }
            if (spriteDescendants >= 2 && hasNonDefaultLayerDescendant && group == null)
                skippedSubtrees++;

            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), defaultLayerId, ref addedGroups, ref touchedRenderers, ref touchedGroups, ref skippedSubtrees);
        }

        static void SyncOrder(Transform t, ref int rCount, ref int gCount)
        {
            var sr = t.GetComponent<SpriteRenderer>();
            var sg = t.GetComponent<SortingGroup>();
            if (t.parent != null)
            {
                int idx = t.GetSiblingIndex();
                if (sr != null && sr.sortingOrder != idx) { sr.sortingOrder = idx; rCount++; }
                if (sg != null && sg.sortingOrder != idx) { sg.sortingOrder = idx; gCount++; }
            }
            for (int i = 0; i < t.childCount; i++) SyncOrder(t.GetChild(i), ref rCount, ref gCount);
        }
    }
}
