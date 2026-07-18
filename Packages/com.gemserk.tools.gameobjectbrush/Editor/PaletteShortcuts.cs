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

        // ================ Nudge selected scene objects (arrow keys) ================
        //
        // Figma-style arrow-key nudge for currently-selected scene GameObjects.
        //   Arrow          =  1 unit
        //   Shift+Arrow    = 10 units
        //   Ctrl+Arrow     = 0.1 units
        // Registered as separate bindings because Unity's Shortcut Manager treats each
        // (key + modifier) pair as its own action.

        [Shortcut("Object Palette/Nudge Right", KeyCode.RightArrow)]
        static void NudgeRight() => Nudge(new Vector3(1f, 0f, 0f));
        [Shortcut("Object Palette/Nudge Left", KeyCode.LeftArrow)]
        static void NudgeLeft() => Nudge(new Vector3(-1f, 0f, 0f));
        [Shortcut("Object Palette/Nudge Up", KeyCode.UpArrow)]
        static void NudgeUp() => Nudge(new Vector3(0f, 1f, 0f));
        [Shortcut("Object Palette/Nudge Down", KeyCode.DownArrow)]
        static void NudgeDown() => Nudge(new Vector3(0f, -1f, 0f));

        [Shortcut("Object Palette/Nudge Right (10u)", KeyCode.RightArrow, ShortcutModifiers.Shift)]
        static void NudgeRightBig() => Nudge(new Vector3(10f, 0f, 0f));
        [Shortcut("Object Palette/Nudge Left (10u)", KeyCode.LeftArrow, ShortcutModifiers.Shift)]
        static void NudgeLeftBig() => Nudge(new Vector3(-10f, 0f, 0f));
        [Shortcut("Object Palette/Nudge Up (10u)", KeyCode.UpArrow, ShortcutModifiers.Shift)]
        static void NudgeUpBig() => Nudge(new Vector3(0f, 10f, 0f));
        [Shortcut("Object Palette/Nudge Down (10u)", KeyCode.DownArrow, ShortcutModifiers.Shift)]
        static void NudgeDownBig() => Nudge(new Vector3(0f, -10f, 0f));

        [Shortcut("Object Palette/Nudge Right (0.1u)", KeyCode.RightArrow, ShortcutModifiers.Control)]
        static void NudgeRightSmall() => Nudge(new Vector3(0.1f, 0f, 0f));
        [Shortcut("Object Palette/Nudge Left (0.1u)", KeyCode.LeftArrow, ShortcutModifiers.Control)]
        static void NudgeLeftSmall() => Nudge(new Vector3(-0.1f, 0f, 0f));
        [Shortcut("Object Palette/Nudge Up (0.1u)", KeyCode.UpArrow, ShortcutModifiers.Control)]
        static void NudgeUpSmall() => Nudge(new Vector3(0f, 0.1f, 0f));
        [Shortcut("Object Palette/Nudge Down (0.1u)", KeyCode.DownArrow, ShortcutModifiers.Control)]
        static void NudgeDownSmall() => Nudge(new Vector3(0f, -0.1f, 0f));

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
