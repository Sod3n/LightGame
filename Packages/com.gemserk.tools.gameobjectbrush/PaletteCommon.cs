using System.Collections.Generic;
using UnityEngine;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    public static class PaletteCommon
    {
        private static PaletteToolMode _mode = PaletteToolMode.Paint;

        // Raises onQuickChanged on change so every UI surface (palette window toggle, scene-view
        // overlay button) stays in sync no matter which of the several entry points (hotkey,
        // Shortcut Manager, overlay button) changed it.
        public static PaletteToolMode mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                _mode = value;
                onQuickChanged?.Invoke();
            }
        }

        public static IBrush brush;
        public static PaletteSelection selection = new PaletteSelection();

        // Paint target: where new painted objects are parented. Persisted per Unity session
        // via globalObjectId in EditorPrefs (see GameObjectPaletteWindow).
        public static Transform paintTarget;

        // Hotkey-controlled offsets applied to preview + new paints.
        public static float paintRotationDegrees;
        public static float paintScaleMultiplier = 1f;

        // Remembered for "Alt+click = duplicate last painted" so the user can keep placing
        // even after closing the palette window or clearing selection.
        public static PaletteObject lastPaintedEntry;

        // The most recently painted scene GameObject, for "drag-out to rotate" (RMB drag on
        // the just-painted object rotates it around its origin, like Figma's rotate handle).
        public static GameObject lastPaintedGameObject;

        // Last world position the brush was moved to (for the palette window status bar).
        public static Vector2 lastCursorWorld;

        // Recently-painted palette entries (most-recent first), capped at RecentCap.
        // Session-scope. Ordering: front = most recent.
        public const int RecentCap = 8;
        public static readonly List<PaletteObject> recentEntries = new List<PaletteObject>();

        // Pinned "favorite" prefab GUIDs. Persists across sessions via EditorPrefs; the
        // palette window loads/saves it. Kept as GUIDs so it survives asset moves.
        public static readonly HashSet<string> favoriteGuids = new HashSet<string>();

        // Minimum world-distance the cursor must travel during a paint-drag before another
        // paint fires. 0 = every drag event paints (spammy). 1u = Figma-like spacing.
        public static float dragSpacing = 1f;

        public static void RememberRecent(PaletteObject entry)
        {
            if (entry == null) return;
            recentEntries.RemoveAll(e => e == null || ReferenceEquals(e.sourceObject, entry.sourceObject));
            recentEntries.Insert(0, entry);
            while (recentEntries.Count > RecentCap) recentEntries.RemoveAt(recentEntries.Count - 1);
            onQuickChanged?.Invoke();
        }

        // Fired whenever recents, favorites, or the active selection changes. UI surfaces
        // (palette window Quick strip, scene-view PaletteQuickOverlay) subscribe to keep
        // themselves in sync without polling.
        public static event System.Action onQuickChanged;

        public static void RaiseQuickChanged() => onQuickChanged?.Invoke();

        // Shared Paint/Erase switch: destroys the preview when entering Erase, recreates it
        // when returning to Paint with a live selection. Callers that also need to repaint the
        // Scene View (this file is runtime-compiled, no UnityEditor access) do that themselves.
        public static void SetMode(PaletteToolMode newMode)
        {
            if (mode == newMode) return;
            mode = newMode;
            if (newMode == PaletteToolMode.Erase)
            {
                brush?.DestroyPreview();
            }
            else if (brush != null && !selection.IsEmpty)
            {
                brush.CreatePreview(selection.selection);
            }
        }
    }
}
