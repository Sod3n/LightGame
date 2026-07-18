using System.Collections.Generic;
using UnityEngine;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    public static class PaletteCommon
    {
        public static PaletteToolMode mode = PaletteToolMode.Paint;
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
        }
    }
}
