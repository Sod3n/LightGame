using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    [EditorTool("Platform Tool")]
    public class PalettePaintTool : EditorTool
    {
        [SerializeField]
        private Texture2D m_ToolIcon = null;

        private GUIContent m_IconContent;

        public const float RotationStepDegrees = 15f;
        public const float ScaleStep = 0.1f;
        public const float MinScale = 0.1f;
        public const float MaxScale = 10f;

        private void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = m_ToolIcon,
                text = "GameObject Palette Tool",
                tooltip = "GameObject Palette Tool"
            };
        }

        public override GUIContent toolbarIcon => m_IconContent;

        private bool leftMouseButtonDown;
        private Vector2? lastPaintedWorld;
        private int paintStrokeUndoGroup = -1;

        public override void OnToolGUI(EditorWindow window)
        {
            var evt = Event.current;
            var p = evt.mousePosition;
            var rawEvent = evt.rawType;

            if (rawEvent == EventType.MouseMove || (rawEvent == EventType.MouseDrag && evt.button == 0))
            {
                var ray = HandleUtility.GUIPointToWorldRay(p);
                var position = ray.origin;
                position.z = 0;
                PaletteCommon.brush.UpdatePosition(position);
            }

            HandlePaintHotkeys(evt);

            if (evt.type == EventType.Layout && leftMouseButtonDown)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            }

            var painting = IsPaintTriggeringEvent(evt);

            if (rawEvent == EventType.MouseDown && evt.button == 0)
            {
                leftMouseButtonDown = true;
                lastPaintedWorld = null;
                // Start a fresh undo group for this stroke so Ctrl+Z reverts the whole sweep.
                Undo.IncrementCurrentGroup();
                paintStrokeUndoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(PaletteCommon.mode == PaletteToolMode.Paint ? "Paint Stroke" : "Erase Stroke");
            }
            if (rawEvent == EventType.MouseUp && evt.button == 0)
            {
                leftMouseButtonDown = false;
                lastPaintedWorld = null;
                if (paintStrokeUndoGroup >= 0)
                {
                    Undo.CollapseUndoOperations(paintStrokeUndoGroup);
                    paintStrokeUndoGroup = -1;
                }
            }

            if (painting)
            {
                if (PaletteCommon.mode == PaletteToolMode.Paint)
                {
                    if (PaletteCommon.brush != null && !PaletteCommon.selection.IsEmpty)
                    {
                        // On MouseDown always paint. On MouseDrag only paint if the cursor
                        // has traveled at least dragSpacing world units since the last paint.
                        var world = PaletteCommon.lastCursorWorld;
                        var shouldPaint = ShouldPaintAt(rawEvent, world, lastPaintedWorld, PaletteCommon.dragSpacing);
                        if (shouldPaint)
                        {
                            PaletteCommon.brush.Paint();
                            lastPaintedWorld = world;
                            if (PaletteCommon.brush.RegenerateOnPaint)
                                PaletteCommon.brush.CreatePreview(PaletteCommon.selection.selection);
                            evt.Use();
                        }
                    }
                }
                else if (PaletteCommon.mode == PaletteToolMode.Erase)
                {
                    var go = HandleUtility.PickGameObject(p, true);
                    if (go != null
                        && PrefabUtility.GetPrefabInstanceStatus(go) != PrefabInstanceStatus.NotAPrefab
                        && IsUnderPaintTarget(go.transform))
                    {
                        Undo.DestroyObjectImmediate(go);
                        evt.Use();
                    }
                }
            }
        }

        // Erase is scoped to whatever's under Paint Target (the same root new paints are
        // parented under) so an erase click can't reach into unrelated parts of the scene —
        // e.g. the player, managers, or another level's objects. No Paint Target set = nothing
        // to scope to, so erase is a no-op rather than falling back to scene-wide deletion.
        public static bool IsUnderPaintTarget(Transform t)
        {
            var root = PaletteCommon.paintTarget;
            if (root == null) return false;
            for (; t != null; t = t.parent)
                if (t == root) return true;
            return false;
        }

        // Testable predicate for drag-to-paint spacing:
        //   - MouseDown: always paint (start of stroke)
        //   - MouseDrag: paint only when cursor has moved past `spacing` world units
        //   - Anything else: don't paint (paint gate is upstream via IsPaintTriggeringEvent)
        public static bool ShouldPaintAt(EventType raw, Vector2 currentWorld, Vector2? lastPaintedWorld, float spacing)
        {
            if (raw == EventType.MouseDown) return true;
            if (raw != EventType.MouseDrag) return false;
            if (!lastPaintedWorld.HasValue) return true;
            if (spacing <= 0f) return true;
            return Vector2.Distance(currentWorld, lastPaintedWorld.Value) >= spacing;
        }

        // Pure predicate: is this event one that should trigger a paint stroke?
        // Kept static + input-only so it's trivially testable without a live SceneView.
        public static bool IsPaintTriggeringEvent(Event evt)
        {
            if (evt == null) return false;
            var t = evt.rawType;
            return (t == EventType.MouseDown || t == EventType.MouseDrag) && evt.button == 0;
        }

        // Also called from GameObjectPaletteWindow.OnSceneViewGui so hotkeys work
        // whenever the palette window is visible, not just when the palette tool is active.
        public static bool HandlePaintHotkeys(Event evt)
        {
            if (evt.rawType != EventType.KeyDown) return false;

            switch (evt.keyCode)
            {
                case KeyCode.E:
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
                    evt.Use();
                    return true;

                case KeyCode.LeftBracket:
                case KeyCode.Comma:
                    PaletteCommon.paintRotationDegrees -= RotationStepDegrees;
                    ApplyPreviewTransform();
                    evt.Use();
                    return true;

                case KeyCode.RightBracket:
                case KeyCode.Period:
                    PaletteCommon.paintRotationDegrees += RotationStepDegrees;
                    ApplyPreviewTransform();
                    evt.Use();
                    return true;

                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    PaletteCommon.paintScaleMultiplier = Mathf.Clamp(
                        PaletteCommon.paintScaleMultiplier - ScaleStep, MinScale, MaxScale);
                    ApplyPreviewTransform();
                    evt.Use();
                    return true;

                case KeyCode.Equals:
                case KeyCode.Plus:
                case KeyCode.KeypadPlus:
                    PaletteCommon.paintScaleMultiplier = Mathf.Clamp(
                        PaletteCommon.paintScaleMultiplier + ScaleStep, MinScale, MaxScale);
                    ApplyPreviewTransform();
                    evt.Use();
                    return true;

                case KeyCode.Alpha0:
                case KeyCode.Keypad0:
                    PaletteCommon.paintRotationDegrees = 0f;
                    PaletteCommon.paintScaleMultiplier = 1f;
                    ApplyPreviewTransform();
                    evt.Use();
                    return true;
            }
            return false;
        }

        private static void ApplyPreviewTransform()
        {
            var brush = PaletteCommon.brush as ScriptableBrushBaseAsset;
            brush?.ApplyPreviewTransform();
        }
    }
}
