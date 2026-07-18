using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    // Global shortcuts registered via Unity's Shortcut Manager. Users can rebind them
    // via Edit > Shortcuts > "Object Palette/*".
    public static class PaletteShortcuts
    {
        // ================ Rotation / Scale ================

        [Shortcut("Object Palette/Rotate CCW 15°", KeyCode.LeftBracket)]
        static void RotateCCW()
        {
            if (!GuardBrushActive()) return;
            PaletteCommon.paintRotationDegrees -= 15f;
            Apply();
        }

        [Shortcut("Object Palette/Rotate CW 15°", KeyCode.RightBracket)]
        static void RotateCW()
        {
            if (!GuardBrushActive()) return;
            PaletteCommon.paintRotationDegrees += 15f;
            Apply();
        }

        [Shortcut("Object Palette/Scale Down 10%", KeyCode.Minus)]
        static void ScaleDown()
        {
            if (!GuardBrushActive()) return;
            PaletteCommon.paintScaleMultiplier = Mathf.Clamp(
                PaletteCommon.paintScaleMultiplier - 0.1f, 0.1f, 10f);
            Apply();
        }

        [Shortcut("Object Palette/Scale Up 10%", KeyCode.Equals)]
        static void ScaleUp()
        {
            if (!GuardBrushActive()) return;
            PaletteCommon.paintScaleMultiplier = Mathf.Clamp(
                PaletteCommon.paintScaleMultiplier + 0.1f, 0.1f, 10f);
            Apply();
        }

        [Shortcut("Object Palette/Reset Rotation & Scale", KeyCode.Alpha0)]
        static void Reset()
        {
            if (!GuardBrushActive()) return;
            PaletteCommon.paintRotationDegrees = 0f;
            PaletteCommon.paintScaleMultiplier = 1f;
            Apply();
        }

        [Shortcut("Object Palette/Toggle Erase Mode", KeyCode.E)]
        static void ToggleErase()
        {
            if (!GuardBrushActive()) return;
            if (PaletteCommon.mode == PaletteToolMode.Paint)
            {
                PaletteCommon.mode = PaletteToolMode.Erase;
                PaletteCommon.brush?.DestroyPreview();
            }
            else
            {
                PaletteCommon.mode = PaletteToolMode.Paint;
                if (PaletteCommon.brush != null && !PaletteCommon.selection.IsEmpty)
                    PaletteCommon.brush.CreatePreview(PaletteCommon.selection.selection);
            }
            SceneView.RepaintAll();
        }

        // ================ Set Paint Under from current scene selection ================

        [Shortcut("Object Palette/Set Paint Under From Selection", KeyCode.U, ShortcutModifiers.Shift | ShortcutModifiers.Action)]
        static void SetPaintUnderFromSelectionShortcut() => SetPaintUnderFromSelection();

        // Public + static for testing and for the "Use Selection" button in the palette
        // window to share the same code path.
        public static bool SetPaintUnderFromSelection()
        {
            var t = Selection.activeTransform;
            if (t == null || !t.gameObject.scene.IsValid()) return false;
            PaletteCommon.paintTarget = t;
            EditorPrefs.SetString("Gemserk.ObjectPalette.PaintTargetId",
                GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString());
            foreach (var w in Resources.FindObjectsOfTypeAll<GameObjectPaletteWindow>())
                w.Repaint();
            return true;
        }

        // ================ V = Select mode (exit paint) ================

        [Shortcut("Object Palette/Exit To Select Mode", KeyCode.V)]
        static void ExitToSelectMode()
        {
            if (!GameObjectPaletteWindow.windowVisible) return;
            PaletteCommon.brush?.DestroyPreview();
            PaletteCommon.selection.Clear();
            var tm = UnityEditor.EditorTools.ToolManager.activeToolType;
            if (tm == typeof(PalettePaintTool))
                UnityEditor.EditorTools.ToolManager.RestorePreviousTool();
            SceneView.RepaintAll();
        }

        // ================ Nudge helper (no default arrow-key bindings) ================
        //
        // Arrow-key bindings were removed because they conflict with Unity's built-in
        // scene navigation. The Nudge helper stays public so users can rebind it via
        // Edit > Shortcuts if they want, and so the copy-properties/undo tests keep
        // exercising the same code path.

        [Shortcut("Object Palette/Nudge Right", KeyCode.None)]
        static void NudgeRight() => Nudge(new Vector3(1f, 0f, 0f));
        [Shortcut("Object Palette/Nudge Left", KeyCode.None)]
        static void NudgeLeft() => Nudge(new Vector3(-1f, 0f, 0f));
        [Shortcut("Object Palette/Nudge Up", KeyCode.None)]
        static void NudgeUp() => Nudge(new Vector3(0f, 1f, 0f));
        [Shortcut("Object Palette/Nudge Down", KeyCode.None)]
        static void NudgeDown() => Nudge(new Vector3(0f, -1f, 0f));

        // ================ Shared helpers ================

        // Only apply nudges when the palette window is visible (Figma-esque contextual mode).
        // Public + static so tests exercise the same code path Unity's shortcut manager calls.
        public static int Nudge(Vector3 worldDelta)
        {
            if (!GameObjectPaletteWindow.windowVisible) return 0;
            var transforms = Selection.transforms;
            if (transforms == null || transforms.Length == 0) return 0;
            int moved = 0;
            foreach (var t in transforms)
            {
                if (t == null) continue;
                if (!t.gameObject.scene.IsValid()) continue; // skip project assets
                Undo.RecordObject(t, "Nudge");
                t.position += worldDelta;
                moved++;
            }
            if (moved > 0) SceneView.RepaintAll();
            return moved;
        }

        // ================ Copy component values across scene selection ================

        [Shortcut("Object Palette/Apply Active's Properties To Selection", KeyCode.D, ShortcutModifiers.Shift | ShortcutModifiers.Action)]
        static void ApplyActiveToSelection()
        {
            ApplyPropertiesToSelection();
        }

        // Public + static for testing. Uses Selection.activeTransform as source and copies
        // its non-Transform component values into every other selected scene transform.
        // Returns number of successful targets updated.
        public static int ApplyPropertiesToSelection()
        {
            var source = Selection.activeTransform;
            var all = Selection.transforms;
            if (source == null || all == null || all.Length < 2) return 0;
            var updated = 0;
            foreach (var target in all)
            {
                if (target == null || target == source) continue;
                if (!target.gameObject.scene.IsValid()) continue;
                CopyOverridesTo(source, target);
                updated++;
            }
            if (updated > 0) SceneView.RepaintAll();
            return updated;
        }

        static void CopyOverridesTo(Transform src, Transform dst)
        {
            var srcComps = src.GetComponents<Component>();
            var dstComps = dst.GetComponents<Component>();
            int n = Mathf.Min(srcComps.Length, dstComps.Length);
            for (int i = 0; i < n; i++)
            {
                var s = srcComps[i]; var d = dstComps[i];
                if (s == null || d == null || s.GetType() != d.GetType()) continue;
                if (s is Transform) continue; // Never copy world transform
                Undo.RecordObject(d, "Apply Properties");
                UnityEditorInternal.ComponentUtility.CopyComponent(s);
                UnityEditorInternal.ComponentUtility.PasteComponentValues(d);
            }
        }

        // Guards for shortcuts that need a live brush selection (rotate/scale/erase).
        static bool GuardBrushActive()
        {
            return GameObjectPaletteWindow.windowVisible
                   && PaletteCommon.brush != null
                   && !PaletteCommon.selection.IsEmpty;
        }

        static void Apply()
        {
            (PaletteCommon.brush as ScriptableBrushBaseAsset)?.ApplyPreviewTransform();
            SceneView.RepaintAll();
        }
    }
}
