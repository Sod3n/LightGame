using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    // "Where and with what we work": while enabled, hides every scene object that isn't the
    // current Paint Target (or one of its descendants) via Unity's built-in Scene Visibility
    // system — the same non-destructive, editor-only mechanism behind Edit > Isolate. Never
    // touches scene data or materials. Also outlines the target's hierarchy in the Scene View
    // so it's obvious what's still visible/paintable/erasable.
    //
    // Self-contained: subscribes to SceneView.duringSceneGui / EditorApplication.update once at
    // load (both no-op while disabled) so it works whenever the Quick overlay is visible,
    // independent of whether the main Palette window is open.
    [InitializeOnLoad]
    public static class PaletteFocusMode
    {
        public static bool Enabled { get; private set; }

        // Roots we hid ourselves — only these get shown again on disable, so we never reveal
        // something the user had already hidden via the Hierarchy eye icon before toggling this on.
        private static readonly List<GameObject> hiddenByUs = new List<GameObject>();
        private static readonly List<Renderer> highlightRenderers = new List<Renderer>();
        private static Transform lastAppliedTarget;

        static PaletteFocusMode()
        {
            SceneView.duringSceneGui += DrawHighlights;
            EditorApplication.update += PollTargetChange;
        }

        public static void SetEnabled(bool value)
        {
            if (Enabled == value) return;
            Enabled = value;
            if (Enabled) Apply();
            else Clear();
            SceneView.RepaintAll();
        }

        private static void PollTargetChange()
        {
            if (!Enabled) return;
            if (PaletteCommon.paintTarget == lastAppliedTarget) return;
            Apply();
        }

        private static void Apply()
        {
            Clear();
            var target = PaletteCommon.paintTarget;
            lastAppliedTarget = target;
            if (target == null) return;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    HideExceptTargetRecursive(root.transform, target);
            }

            highlightRenderers.AddRange(target.GetComponentsInChildren<Renderer>(true));
            SceneView.RepaintAll();
        }

        private static void Clear()
        {
            foreach (var go in hiddenByUs)
                if (go != null) SceneVisibilityManager.instance.Show(go, true);
            hiddenByUs.Clear();
            highlightRenderers.Clear();
            lastAppliedTarget = null;
        }

        private static void HideExceptTargetRecursive(Transform t, Transform target)
        {
            if (t == target) return; // target's own subtree stays fully visible, untouched
            if (IsAncestorOf(t, target))
            {
                // t is on the path down to target — must stay visible itself, but its OTHER
                // children (siblings of the next step toward target) still need hiding.
                foreach (Transform child in t)
                    HideExceptTargetRecursive(child, target);
                return;
            }
            if (SceneVisibilityManager.instance.IsHidden(t.gameObject)) return; // already hidden by the user — leave it, don't track it
            SceneVisibilityManager.instance.Hide(t.gameObject, true);
            hiddenByUs.Add(t.gameObject);
        }

        private static bool IsAncestorOf(Transform maybeAncestor, Transform t)
        {
            for (var p = t.parent; p != null; p = p.parent)
                if (p == maybeAncestor) return true;
            return false;
        }

        private static void DrawHighlights(SceneView sv)
        {
            if (!Enabled) return;
            var target = PaletteCommon.paintTarget;
            if (target == null) return;

            Handles.color = new Color(0.3f, 1f, 0.4f, 0.9f);
            var any = false;
            foreach (var r in highlightRenderers)
            {
                if (r == null) continue;
                any = true;
                var b = r.bounds;
                Handles.DrawWireCube(b.center, b.size);
            }
            if (!any)
            {
                // No renderers under the target (e.g. an empty grouping transform) — still
                // show something so it's clear where "here" is.
                Handles.DrawWireDisc(target.position, Vector3.forward, 0.5f);
            }
        }
    }
}
