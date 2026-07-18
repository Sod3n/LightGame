using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    // Palette window — rewritten in UI Toolkit. All static/public helpers used by tests and
    // scene-view hooks are kept identical; only the presentation layer moved from IMGUI to
    // VisualElements. Scene-view event routing (SceneView.duringSceneGui, OnBecameVisible)
    // still uses the same C# hooks — those pass Event objects, not GUI drawing.
    public class GameObjectPaletteWindow : EditorWindow
    {
        [MenuItem("Window/Object Palette/Palette Window")]
        public static void OpenWindow()
        {
            var window = GetWindow(typeof(GameObjectPaletteWindow), false, "Object Palette");
            window.minSize = new Vector2(300, 300);
        }

        // Hierarchy-click → paint under. Ignored for programmatic Selection changes:
        // EditorWindow.mouseOverWindow only points to Hierarchy/SceneView when a real
        // user mouse gesture is in progress. Our own SelectBrushObject sets Selection
        // while the mouse is over the palette window, so it doesn't trip this hook.
        [InitializeOnLoadMethod]
        static void HookSelectionChanged()
        {
            Selection.selectionChanged -= OnSelectionMaybeSetPaintUnder;
            Selection.selectionChanged += OnSelectionMaybeSetPaintUnder;
        }

        static void OnSelectionMaybeSetPaintUnder()
        {
            if (!ShouldSetPaintUnderFromSelection(Selection.activeTransform))
                return;

            var t = Selection.activeTransform;
            PaletteCommon.paintTarget = t;
            EditorPrefs.SetString("Gemserk.ObjectPalette.PaintTargetId",
                GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString());
            foreach (var w in Resources.FindObjectsOfTypeAll<GameObjectPaletteWindow>())
                w.Repaint();
        }

        // Suppression counter for programmatic Selection changes. Incremented by
        // SelectBrushObject when we set Selection to the preview (so the user can inspect
        // it), decremented on the next editor tick. Any user-driven Selection change happens
        // while this is zero and passes the guard.
        public static int suppressSelectionHookCount;

        public static void BeginSuppressSelectionHook()
        {
            suppressSelectionHookCount++;
            EditorApplication.delayCall += EndSuppressSelectionHookOnce;
        }
        static void EndSuppressSelectionHookOnce()
        {
            if (suppressSelectionHookCount > 0) suppressSelectionHookCount--;
        }

        // Pure decision function — testable without a live Hierarchy window.
        public static bool ShouldSetPaintUnderFromSelection(Transform selection)
        {
            if (!windowVisible || !autoSetPaintUnderFromHierarchyClick) return false;
            if (suppressSelectionHookCount > 0) return false;
            if (selection == null || !selection.gameObject.scene.IsValid()) return false;
            if (selection.GetComponentInParent<BrushPreview>() != null) return false;
            if (PaletteCommon.paintTarget == selection) return false;
            return true;
        }

        public static bool windowVisible = false;

        // When true, clicking a scene GameObject in the Hierarchy (or picking one in the
        // scene view) automatically sets it as the Paint Under target. Ignores
        // programmatic selection changes (like our SelectBrushObject setting the preview
        // as active) — see the mouseOverWindow guard in HookSelectionChanged.
        public static bool autoSetPaintUnderFromHierarchyClick = true;

        [SerializeField]
        private ScriptableBrushBaseAsset defaultBrush = null;

        private const string PaintTargetPrefKey = "Gemserk.ObjectPalette.PaintTargetId";
        private const string FavoritesPrefKey = "Gemserk.ObjectPalette.Favorites";

        private List<ObjectPaletteBaseAsset> availablePalettes = new List<ObjectPaletteBaseAsset>();
        private List<ScriptableBrushBaseAsset> availableBrushes = new List<ScriptableBrushBaseAsset>();
        private int selectedPaletteIndex;
        private List<PaletteObject> cachedEntries;

        // UI containers we rebuild on data changes
        private DropdownField brushDropdown;
        private DropdownField paletteDropdown;
        private ObjectField paintTargetField;
        private FloatField dragSpacingField;
        private FloatField rotField;
        private FloatField scaleField;
        private VisualElement favoritesContainer;
        private VisualElement paletteGrid;
        private Slider previewSizeSlider;
        private Foldout hotkeysFoldout;
        private Label statusBar;

        // Preview size (persisted implicitly via serialized field on the window)
        [SerializeField] private float currentButtonSize = 72f;

        // ==================== Lifecycle ====================

        private void OnEnable()
        {
            ReloadPalettesAndBrushes();
            EditorSceneManager.sceneOpened += OnSceneOpened;
            DestroyHangingPreview();
            if (PaletteCommon.brush == null) PaletteCommon.brush = defaultBrush;
            RestorePaintTarget();
            LoadFavorites();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            PaletteCommon.brush?.DestroyPreview();
        }

        private void OnBecameVisible()
        {
            windowVisible = true;
            SceneView.beforeSceneGui += OnBeforeSceneGui;
            SceneView.duringSceneGui += OnSceneViewGui;
            if (!PaletteCommon.selection.IsEmpty)
                PaletteCommon.brush?.CreatePreview(PaletteCommon.selection.selection);
            EditorApplication.update += Tick;
        }

        private void OnBecameInvisible()
        {
            windowVisible = false;
            SceneView.duringSceneGui -= OnSceneViewGui;
            SceneView.beforeSceneGui -= OnBeforeSceneGui;
            EditorApplication.update -= Tick;
            PaletteCommon.brush?.DestroyPreview();
        }

        private void OnFocus() => ReloadPalettesAndBrushes();

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single) UnselectPalette();
            RestorePaintTarget();
        }

        private void DestroyHangingPreview()
        {
            var scenes = EditorSceneManager.sceneCount;
            for (var i = 0; i < scenes; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var hp in root.GetComponentsInChildren<BrushPreview>())
                        DestroyImmediate(hp.gameObject);
            }
        }

        // Periodic UI refresh for status bar (cursor world coords change with mouse move)
        // and to catch external changes to PaletteCommon.paintTarget (e.g. from the
        // "Set Paint Under From Selection" shortcut).
        private Transform lastKnownPaintTarget;
        private void Tick()
        {
            if (statusBar != null) statusBar.text = BuildStatusText();
            if (paintTargetField != null && PaletteCommon.paintTarget != lastKnownPaintTarget)
            {
                RebuildPaintTargetField();
                lastKnownPaintTarget = PaletteCommon.paintTarget;
            }
        }

        // ==================== Scene-view hooks (unchanged from IMGUI version) ====================

        private void OnSceneViewGui(SceneView sceneView)
        {
            if (PalettePaintTool.HandlePaintHotkeys(Event.current))
            {
                sceneView.Repaint();
                Repaint();
                return;
            }

            // Alt+LMB: hover an existing scene object → clone that; empty space → duplicate last painted.
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && Event.current.alt)
            {
                var world = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
                world.z = 0f;
                var hovered = HandleUtility.PickGameObject(Event.current.mousePosition, false);
                if (hovered != null && hovered.scene.IsValid())
                {
                    CloneExistingAt(hovered, world);
                    Event.current.Use();
                    sceneView.Repaint();
                    return;
                }
                if (PaletteCommon.lastPaintedEntry != null && PaletteCommon.brush != null)
                {
                    DuplicateLastPaintedAt(world);
                    Event.current.Use();
                    sceneView.Repaint();
                    return;
                }
            }

            HandleRmbRotateDrag(sceneView);

            Handles.BeginGUI();
            var r = sceneView.camera.pixelRect;
            var toolsRect = new Rect(r.xMax - 100, r.yMax - 50, 75, 50);
            GUILayout.BeginArea(toolsRect);
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var eraseToggle = GUILayout.Toggle(PaletteCommon.mode == PaletteToolMode.Erase, "Erase", "Button");
            if (EditorGUI.EndChangeCheck())
            {
                if (eraseToggle)
                {
                    PaletteCommon.mode = PaletteToolMode.Erase;
                    UnselectUnityTool();
                    PaletteCommon.brush?.DestroyPreview();
                }
                else
                {
                    PaletteCommon.mode = PaletteToolMode.Paint;
                    RestoreUnityTool();
                    if (PaletteCommon.brush != null && !PaletteCommon.selection.IsEmpty)
                        PaletteCommon.brush.CreatePreview(PaletteCommon.selection.selection);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();

            if (Event.current.rawType == EventType.KeyUp && Event.current.keyCode == KeyCode.Escape)
            {
                UnselectPalette();
                Repaint();
            }

            if (Event.current.rawType == EventType.MouseUp && Event.current.button == 1)
            {
                if (!rmbWasDrag)
                {
                    UnselectPalette();
                    Repaint();
                }
                rmbWasDrag = false;
                rmbRotateTarget = null;
            }
        }

        private void OnBeforeSceneGui(SceneView view)
        {
            if (Event.current.type == EventType.ScrollWheel && Event.current.control)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                PaletteCommon.brush.CreatePreview(PaletteCommon.selection.selection);
                Event.current.Use();
            }
        }

        // ==================== UI Toolkit construction ====================

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 4;
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;

            // Top header — brush/palette pickers, paint-target, transform offsets, layers, favorites
            var header = BuildHeaderSection();
            header.style.flexShrink = 0;
            root.Add(header);

            // Palette grid — scrollable region that gets whatever height remains after the
            // fixed header and footer. min-height keeps it usable when the window is short.
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "palette-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 80;
            scroll.style.overflow = Overflow.Hidden;
            paletteGrid = new VisualElement { name = "palette-grid" };
            scroll.Add(paletteGrid);
            root.Add(scroll);

            // Preview-size slider
            previewSizeSlider = new Slider("Preview Size", 32f, 200f) { value = currentButtonSize };
            previewSizeSlider.RegisterValueChangedCallback(evt =>
            {
                currentButtonSize = evt.newValue;
                RebuildPaletteGrid();
                RebuildFavoritesStrip();
            });
            root.Add(previewSizeSlider);

            // Hotkeys hint foldout
            hotkeysFoldout = new Foldout { text = "Hotkeys (rebind in Edit > Shortcuts)", value = false };
            AddHotkeyLabel("LMB              — paint (MMB reserved for camera pan)");
            AddHotkeyLabel("Alt + LMB        — duplicate last painted (bypasses palette)");
            AddHotkeyLabel("[  ]             — rotate ±15°");
            AddHotkeyLabel("-  =             — scale ±10%");
            AddHotkeyLabel("0                — reset rotation & scale");
            AddHotkeyLabel("E                — toggle erase mode");
            AddHotkeyLabel("V                — exit paint mode, back to Move tool");
            AddHotkeyLabel("RMB drag         — rotate last painted around its origin");
            AddHotkeyLabel("Ctrl+Shift+D     — apply active's properties to selection");
            AddHotkeyLabel("Ctrl+Shift+U     — set Paint Under = current scene selection");
            AddHotkeyLabel("Esc / RMB click  — deselect palette entry");
            AddHotkeyLabel("Shift+click      — multi-select in palette");
            AddHotkeyLabel("Ctrl+wheel       — regenerate preview");
            root.Add(hotkeysFoldout);

            // Status bar (bottom, sticky)
            statusBar = new Label(BuildStatusText());
            statusBar.style.backgroundColor = new Color(0f, 0f, 0f, 0.10f);
            statusBar.style.fontSize = 10;
            statusBar.style.paddingLeft = 6;
            statusBar.style.paddingTop = 2;
            statusBar.style.paddingBottom = 2;
            statusBar.style.marginTop = 4;
            root.Add(statusBar);

            RebuildAll();
        }

        private void AddHotkeyLabel(string text)
        {
            var l = new Label(text);
            l.style.fontSize = 10;
            l.style.marginLeft = 12;
            hotkeysFoldout.Add(l);
        }

        private VisualElement BuildHeaderSection()
        {
            var header = new VisualElement();

            // Brush dropdown
            brushDropdown = new DropdownField("Brush", new List<string>(), 0);
            brushDropdown.RegisterValueChangedCallback(evt =>
            {
                var idx = brushDropdown.index;
                if (idx >= 0 && idx < availableBrushes.Count)
                    PaletteCommon.brush = availableBrushes[idx];
            });
            header.Add(brushDropdown);

            // Palette dropdown
            paletteDropdown = new DropdownField("Palette", new List<string>(), 0);
            paletteDropdown.RegisterValueChangedCallback(evt =>
            {
                selectedPaletteIndex = paletteDropdown.index;
                cachedEntries = null;
                RebuildPaletteGrid();
            });
            header.Add(paletteDropdown);

            // Paint target
            paintTargetField = new ObjectField("Paint Under") { objectType = typeof(Transform), allowSceneObjects = true };
            paintTargetField.tooltip = "New painted objects become children of this transform.";
            paintTargetField.RegisterValueChangedCallback(evt =>
            {
                PaletteCommon.paintTarget = evt.newValue as Transform;
                SavePaintTarget();
            });
            header.Add(paintTargetField);

            var paintTargetButtons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var useSelBtn = new Button(() =>
            {
                var t = Selection.activeTransform;
                if (t != null && t.gameObject.scene.IsValid())
                {
                    PaletteCommon.paintTarget = t;
                    paintTargetField.SetValueWithoutNotify(t);
                    SavePaintTarget();
                }
            }) { text = "Use Selection" };
            useSelBtn.style.width = 120;
            var clearBtn = new Button(() =>
            {
                PaletteCommon.paintTarget = null;
                paintTargetField.SetValueWithoutNotify(null);
                SavePaintTarget();
            }) { text = "Clear" };
            clearBtn.style.width = 60;
            paintTargetButtons.Add(useSelBtn);
            paintTargetButtons.Add(clearBtn);
            header.Add(paintTargetButtons);

            // Drag spacing
            dragSpacingField = new FloatField("Drag spacing") { value = PaletteCommon.dragSpacing };
            dragSpacingField.tooltip = "World-space distance the cursor must travel during a paint-drag before another paint fires. 0 = every drag paints.";
            dragSpacingField.RegisterValueChangedCallback(evt => PaletteCommon.dragSpacing = Mathf.Max(0f, evt.newValue));
            header.Add(dragSpacingField);

            // "Save Overrides as Variant" — bakes the current preview's per-instance overrides
            // into a new prefab variant on disk and appends it to the active palette.
            var saveVariantBtn = new Button(() => SaveCurrentAsVariant()) { text = "Save Overrides as Variant" };
            saveVariantBtn.tooltip = "Save the current brush preview (with your Inspector edits) as a new prefab variant, and add it to the active palette.";
            header.Add(saveVariantBtn);

            // Rot / Scale row
            var rotScaleRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            rotField = new FloatField("Rot") { value = PaletteCommon.paintRotationDegrees };
            rotField.style.width = 100;
            rotField.RegisterValueChangedCallback(evt =>
            {
                PaletteCommon.paintRotationDegrees = evt.newValue;
                (PaletteCommon.brush as ScriptableBrushBaseAsset)?.ApplyPreviewTransform();
                SceneView.RepaintAll();
            });
            scaleField = new FloatField("Scale") { value = PaletteCommon.paintScaleMultiplier };
            scaleField.style.width = 100;
            scaleField.RegisterValueChangedCallback(evt =>
            {
                PaletteCommon.paintScaleMultiplier = Mathf.Max(0.01f, evt.newValue);
                (PaletteCommon.brush as ScriptableBrushBaseAsset)?.ApplyPreviewTransform();
                SceneView.RepaintAll();
            });
            var resetBtn = new Button(() =>
            {
                PaletteCommon.paintRotationDegrees = 0f;
                PaletteCommon.paintScaleMultiplier = 1f;
                rotField.SetValueWithoutNotify(0f);
                scaleField.SetValueWithoutNotify(1f);
                (PaletteCommon.brush as ScriptableBrushBaseAsset)?.ApplyPreviewTransform();
                SceneView.RepaintAll();
            }) { text = "Reset" };
            resetBtn.style.width = 60;
            rotScaleRow.Add(rotField);
            rotScaleRow.Add(scaleField);
            rotScaleRow.Add(resetBtn);
            header.Add(rotScaleRow);

            // Favorites/recents strip
            var favFoldout = new Foldout { text = "Quick (recent + favorites)", value = true };
            favoritesContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            favFoldout.Add(favoritesContainer);
            header.Add(favFoldout);

            return header;
        }

        // ==================== Data ↔ UI rebuild ====================

        private void RebuildAll()
        {
            RebuildBrushDropdown();
            RebuildPaletteDropdown();
            RebuildPaintTargetField();
            RebuildFavoritesStrip();
            RebuildPaletteGrid();
        }

        private void RebuildBrushDropdown()
        {
            if (brushDropdown == null) return;
            brushDropdown.choices = availableBrushes.Select(b => b.name).ToList();
            var idx = availableBrushes.IndexOf(PaletteCommon.brush as ScriptableBrushBaseAsset);
            if (idx >= 0 && brushDropdown.choices.Count > idx) brushDropdown.SetValueWithoutNotify(brushDropdown.choices[idx]);
        }

        private void RebuildPaletteDropdown()
        {
            if (paletteDropdown == null) return;
            paletteDropdown.choices = availablePalettes.Select(p => p.name).ToList();
            if (paletteDropdown.choices.Count > 0)
            {
                if (selectedPaletteIndex < 0 || selectedPaletteIndex >= paletteDropdown.choices.Count) selectedPaletteIndex = 0;
                paletteDropdown.SetValueWithoutNotify(paletteDropdown.choices[selectedPaletteIndex]);
            }
        }

        private void RebuildPaintTargetField()
        {
            paintTargetField?.SetValueWithoutNotify(PaletteCommon.paintTarget);
        }

        private void RebuildFavoritesStrip()
        {
            if (favoritesContainer == null) return;
            favoritesContainer.Clear();
            if (PaletteCommon.recentEntries.Count == 0 && PaletteCommon.favoriteGuids.Count == 0)
            {
                favoritesContainer.Add(new Label("(paint something to populate)") { style = { unityFontStyleAndWeight = FontStyle.Italic, fontSize = 10, color = Color.gray } });
                return;
            }
            var seen = new HashSet<Object>();
            if (cachedEntries != null)
                foreach (var e in cachedEntries)
                {
                    if (e == null || !IsFavorite(e)) continue;
                    favoritesContainer.Add(BuildQuickButton(e, isFavorite: true));
                    seen.Add(e.sourceObject);
                }
            foreach (var e in PaletteCommon.recentEntries)
            {
                if (e == null || seen.Contains(e.sourceObject)) continue;
                favoritesContainer.Add(BuildQuickButton(e, isFavorite: false));
            }
        }

        private VisualElement BuildQuickButton(PaletteObject entry, bool isFavorite)
        {
            var size = 36f;
            var btn = new Button(() => SelectBrushObject(entry)) { text = isFavorite ? "★" : "" };
            btn.style.width = size;
            btn.style.height = size;
            btn.tooltip = entry.name;
            if (entry.preview == null) entry.preview = AssetPreview.GetAssetPreview(entry.sourceObject);
            if (entry.preview != null)
                btn.style.backgroundImage = new StyleBackground((Texture2D)entry.preview);
            btn.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    ToggleFavorite(entry);
                    RebuildFavoritesStrip();
                    RebuildPaletteGrid();
                    evt.StopPropagation();
                }
            });
            return btn;
        }

        private void RebuildPaletteGrid()
        {
            if (paletteGrid == null) return;
            paletteGrid.Clear();
            if (availablePalettes.Count == 0)
            {
                paletteGrid.Add(new Label("No palettes found in project. Create one via Assets > Create > Object Palette > Palette > GameObject Palette."));
                return;
            }
            if (cachedEntries == null)
            {
                var palette = availablePalettes[selectedPaletteIndex];
                cachedEntries = palette.CreatePaletteObjects();
            }
            var groups = GroupByCategory(cachedEntries);
            foreach (var kv in groups)
            {
                var foldout = new Foldout { text = $"{kv.Key}  ({kv.Value.Count})", value = true };
                var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
                foreach (var entry in kv.Value)
                    grid.Add(BuildEntryButton(entry));
                foldout.Add(grid);
                paletteGrid.Add(foldout);
            }
        }

        private VisualElement BuildEntryButton(PaletteObject entry)
        {
            var size = currentButtonSize;
            var btn = new Button(() => OnPaletteEntryClicked(entry)) { tooltip = entry.name };
            btn.style.width = size;
            btn.style.height = size;
            btn.style.marginRight = 2;
            btn.style.marginBottom = 2;
            if (entry.preview == null) entry.preview = AssetPreview.GetAssetPreview(entry.sourceObject);
            if (entry.preview != null)
                btn.style.backgroundImage = new StyleBackground((Texture2D)entry.preview);

            var label = new Label(entry.name);
            label.style.fontSize = 9;
            label.style.color = Color.white;
            label.style.unityTextAlign = TextAnchor.LowerCenter;
            label.style.textShadow = new TextShadow { offset = new Vector2(1, 1), color = Color.black };
            label.style.position = Position.Absolute;
            label.style.left = 0; label.style.right = 0; label.style.bottom = 0;
            btn.Add(label);

            // Highlight if selected
            if (PaletteCommon.selection.Contains(entry))
                btn.style.backgroundColor = new Color(0.4f, 0.6f, 1f, 0.25f);

            // Star badge if favorite
            if (IsFavorite(entry))
            {
                var star = new Label("★");
                star.style.position = Position.Absolute;
                star.style.top = 0; star.style.left = 2;
                star.style.color = new Color(1f, 0.9f, 0.2f);
                btn.Add(star);
            }

            // Right-click = toggle favorite; shift+click = multi-select
            btn.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    ToggleFavorite(entry);
                    RebuildFavoritesStrip();
                    RebuildPaletteGrid();
                    evt.StopPropagation();
                }
            });
            return btn;
        }

        private void OnPaletteEntryClicked(PaletteObject entry)
        {
            var multiselect = Event.current != null && Event.current.shift;
            var isSelected = PaletteCommon.selection.Contains(entry);
            if (multiselect) SelectBrushObject(entry);
            else if (isSelected) UnselectPalette();
            else { UnselectPalette(); SelectBrushObject(entry); }
            RebuildPaletteGrid();
        }

        // ==================== Selection helpers ====================

        private void UnselectPalette()
        {
            PaletteCommon.brush?.DestroyPreview();
            PaletteCommon.selection.Clear();
            RestoreUnityTool();
        }

        private void SelectBrushObject(PaletteObject o) => SelectBrushObjectStatic(o);

        // Public + static so the Scene View overlay (and other outside callers) share the
        // exact same "start painting this entry" path — auto-brush pick, tool activation,
        // preview creation, inspector selection, hook suppression.
        public static void SelectBrushObjectStatic(PaletteObject o)
        {
            if (o == null) return;
            if (PaletteCommon.brush == null)
            {
                // Try to auto-pick a brush now so the overlay is usable even if the palette
                // window was never opened.
                var brushes = AssetDatabaseExt.FindAssets<ScriptableBrushBaseAsset>();
                if (brushes.Count > 0) PaletteCommon.brush = brushes[0];
            }
            if (PaletteCommon.brush == null)
            {
                Debug.LogError("[Object Palette] No brush asset in project. Create one via Assets > Create > Object Palette > Default Brush.");
                return;
            }
            PaletteCommon.selection.Clear();
            PaletteCommon.selection.Add(o);
            PaletteCommon.brush.CreatePreview(PaletteCommon.selection.selection);
            var tm = UnityEditor.EditorTools.ToolManager.activeToolType;
            if (tm != typeof(PalettePaintTool))
                UnityEditor.EditorTools.ToolManager.SetActiveTool<PalettePaintTool>();
            (PaletteCommon.brush as ScriptableBrushBaseAsset)?.ApplyPreviewTransform();
            var brush = PaletteCommon.brush as ScriptableBrushBaseAsset;
            BeginSuppressSelectionHook();
            if (brush?.previewParent != null && brush.previewParent.childCount > 0)
                Selection.activeGameObject = brush.previewParent.GetChild(0).gameObject;
            else if (o.sourceObject != null)
                Selection.activeObject = o.sourceObject;

            // Force each open palette window to reflect the new selection highlight.
            foreach (var w in Resources.FindObjectsOfTypeAll<GameObjectPaletteWindow>())
                w.Repaint();
        }

        private void UnselectUnityTool()
        {
            var type = UnityEditor.EditorTools.ToolManager.activeToolType;
            if (type != typeof(PalettePaintTool))
                UnityEditor.EditorTools.ToolManager.SetActiveTool<PalettePaintTool>();
        }

        public void RestoreUnityTool()
        {
            var type = UnityEditor.EditorTools.ToolManager.activeToolType;
            if (type == typeof(PalettePaintTool))
                UnityEditor.EditorTools.ToolManager.RestorePreviousTool();
        }

        // ==================== Save preview as new prefab variant ====================

        // Takes the current preview instance (with any inspector overrides the user made) and
        // saves it as a prefab variant of the source prefab. Adds the variant to the active
        // palette. Public for testing and menu/button invocation.
        public string SaveCurrentAsVariant()
        {
            var brush = PaletteCommon.brush as ScriptableBrushBaseAsset;
            if (brush == null || brush.previewParent == null || brush.previewParent.childCount == 0)
            {
                EditorUtility.DisplayDialog("Save Variant", "Select a palette entry first — there's no live preview to save.", "OK");
                return null;
            }
            var preview = brush.previewParent.GetChild(0).gameObject;
            var source = PrefabUtility.GetCorrespondingObjectFromSource(preview);
            if (source == null)
            {
                EditorUtility.DisplayDialog("Save Variant", "Preview isn't a prefab instance — nothing to make a variant of.", "OK");
                return null;
            }

            var sourceDir = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(source))?.Replace('\\', '/') ?? "Assets";
            var variantsDir = sourceDir + "/Variants";
            if (!AssetDatabase.IsValidFolder(variantsDir))
            {
                if (!AssetDatabase.IsValidFolder(sourceDir))
                    variantsDir = "Assets";
                else
                    AssetDatabase.CreateFolder(sourceDir, "Variants");
            }

            var defaultName = source.name + "_variant";
            var chosen = EditorUtility.SaveFilePanelInProject(
                "Save Variant", defaultName, "prefab",
                "Save the brush preview as a new prefab variant.",
                variantsDir);
            if (string.IsNullOrEmpty(chosen)) return null;

            // Reset root transform so variant has a neutral origin (baked scale from the
            // source prefab is preserved because we're only touching root position/rotation).
            var savedPos = preview.transform.localPosition;
            var savedRot = preview.transform.localRotation;
            preview.transform.localPosition = Vector3.zero;
            preview.transform.localRotation = Quaternion.identity;
            GameObject variantAsset;
            try
            {
                variantAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(preview, chosen, InteractionMode.AutomatedAction);
            }
            finally
            {
                preview.transform.localPosition = savedPos;
                preview.transform.localRotation = savedRot;
            }
            if (variantAsset == null) return null;

            // Append the new variant to the current palette so it appears in the grid.
            if (availablePalettes.Count > 0 && selectedPaletteIndex >= 0
                && availablePalettes[selectedPaletteIndex] is ObjectPaletteAsset palette)
            {
                if (palette.prefabs == null) palette.prefabs = new List<GameObject>();
                if (!palette.prefabs.Contains(variantAsset))
                {
                    palette.prefabs.Add(variantAsset);
                    EditorUtility.SetDirty(palette);
                    AssetDatabase.SaveAssets();
                }
            }

            cachedEntries = null;
            RebuildPaletteGrid();
            RebuildFavoritesStrip();
            Debug.Log($"[Object Palette] Saved variant: {chosen}");
            return chosen;
        }

        private void ReloadPalettesAndBrushes()
        {
            availablePalettes = AssetDatabaseExt.FindAssets<ObjectPaletteBaseAsset>();
            availableBrushes = AssetDatabaseExt.FindAssets<ScriptableBrushBaseAsset>();
            // Auto-select first brush if none set — prevents NPE in SelectBrushObject when
            // the user clicks a palette entry before touching the Brush dropdown.
            if (PaletteCommon.brush == null && availableBrushes.Count > 0)
                PaletteCommon.brush = availableBrushes[0];
            RebuildBrushDropdown();
            RebuildPaletteDropdown();
        }

        // ==================== Public static helpers (unchanged API — tests + external use) ====================

        public static void DuplicateLastPaintedAt(Vector3 pos)
        {
            var entry = PaletteCommon.lastPaintedEntry;
            var brush = PaletteCommon.brush as ScriptableBrushBaseAsset;
            if (entry == null || brush == null) return;

            var savedSelection = new List<PaletteObject>(PaletteCommon.selection.selection);

            PaletteCommon.selection.Clear();
            PaletteCommon.selection.Add(entry);
            brush.CreatePreview(PaletteCommon.selection.selection);
            brush.UpdatePosition(pos);
            brush.Paint();
            brush.DestroyPreview();

            PaletteCommon.selection.Clear();
            foreach (var s in savedSelection) PaletteCommon.selection.Add(s);
            if (savedSelection.Count > 0)
                brush.CreatePreview(PaletteCommon.selection.selection);
        }

        public static void ToggleFavorite(PaletteObject entry)
        {
            if (entry?.sourceObject == null) return;
            var path = AssetDatabase.GetAssetPath(entry.sourceObject);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;
            if (!PaletteCommon.favoriteGuids.Add(guid))
                PaletteCommon.favoriteGuids.Remove(guid);
            EditorPrefs.SetString(FavoritesPrefKey, string.Join(";", PaletteCommon.favoriteGuids));
        }

        public static bool IsFavorite(PaletteObject entry)
        {
            if (entry?.sourceObject == null) return false;
            var path = AssetDatabase.GetAssetPath(entry.sourceObject);
            var guid = AssetDatabase.AssetPathToGUID(path);
            return !string.IsNullOrEmpty(guid) && PaletteCommon.favoriteGuids.Contains(guid);
        }

        public static List<KeyValuePair<string, List<PaletteObject>>>
            GroupByCategory(IList<PaletteObject> entries)
        {
            var order = new List<string>();
            var buckets = new Dictionary<string, List<PaletteObject>>();
            if (entries == null) return new List<KeyValuePair<string, List<PaletteObject>>>();
            foreach (var e in entries)
            {
                if (e == null) continue;
                var cat = CategoryFor(e);
                if (!buckets.ContainsKey(cat)) { buckets[cat] = new List<PaletteObject>(); order.Add(cat); }
                buckets[cat].Add(e);
            }
            var result = new List<KeyValuePair<string, List<PaletteObject>>>();
            foreach (var cat in order)
                result.Add(new KeyValuePair<string, List<PaletteObject>>(cat, buckets[cat]));
            return result;
        }

        static string CategoryFor(PaletteObject e)
        {
            if (e?.sourceObject == null) return "General";
            var assetPath = AssetDatabase.GetAssetPath(e.sourceObject);
            if (string.IsNullOrEmpty(assetPath)) return "General";
            var dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir)) return "General";
            var slash = dir.LastIndexOf('/');
            var leaf = slash < 0 ? dir : dir.Substring(slash + 1);
            return string.IsNullOrEmpty(leaf) ? "General" : leaf;
        }

        // ==================== Alt+drag clone + RMB rotate (unchanged) ====================

        public static GameObject CloneExistingAt(GameObject source, Vector3 worldPos)
        {
            if (source == null) return null;
            GameObject clone;
            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(source);
            if (prefabAsset != null)
            {
                clone = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, source.scene);
                clone.transform.SetParent(source.transform.parent, worldPositionStays: false);
                MirrorOverrides(source.transform, clone.transform, isRoot: true);
            }
            else
            {
                clone = (GameObject)Object.Instantiate(source, source.transform.parent);
                clone.name = source.name;
            }
            clone.transform.position = worldPos;
            clone.transform.rotation = source.transform.rotation;
            clone.transform.localScale = source.transform.lossyScale;
            Undo.RegisterCreatedObjectUndo(clone, "Clone Existing");
            return clone;
        }

        static void MirrorOverrides(Transform src, Transform dst, bool isRoot)
        {
            var srcComps = src.GetComponents<Component>();
            var dstComps = dst.GetComponents<Component>();
            int n = Mathf.Min(srcComps.Length, dstComps.Length);
            for (int i = 0; i < n; i++)
            {
                var s = srcComps[i]; var d = dstComps[i];
                if (s == null || d == null || s.GetType() != d.GetType()) continue;
                if (s is Transform && isRoot) continue;
                UnityEditorInternal.ComponentUtility.CopyComponent(s);
                UnityEditorInternal.ComponentUtility.PasteComponentValues(d);
            }
            int childCount = Mathf.Min(src.childCount, dst.childCount);
            for (int i = 0; i < childCount; i++)
                MirrorOverrides(src.GetChild(i), dst.GetChild(i), false);
        }

        private static bool rmbWasDrag;
        private static Transform rmbRotateTarget;
        private static Vector2 rmbInitialWorld;
        private static float rmbInitialRotationZ;

        private void HandleRmbRotateDrag(SceneView sv)
        {
            var evt = Event.current;
            if (evt.button != 1) return;
            var world = HandleUtility.GUIPointToWorldRay(evt.mousePosition).origin;
            world.z = 0f;
            if (evt.type == EventType.MouseDown)
            {
                var tgt = PaletteCommon.lastPaintedGameObject;
                if (tgt != null)
                {
                    rmbRotateTarget = tgt.transform;
                    rmbInitialWorld = world;
                    rmbInitialRotationZ = rmbRotateTarget.eulerAngles.z;
                    rmbWasDrag = false;
                }
            }
            else if (evt.type == EventType.MouseDrag && rmbRotateTarget != null)
            {
                RotateTargetTo(rmbRotateTarget, rmbInitialWorld, world, rmbInitialRotationZ);
                rmbWasDrag = true;
                sv.Repaint();
                evt.Use();
            }
        }

        public static void RotateTargetTo(Transform target, Vector2 dragStartWorld, Vector2 currentWorld, float initialRotationZ)
        {
            if (target == null) return;
            var pivot = (Vector2)target.position;
            var a0 = Mathf.Atan2(dragStartWorld.y - pivot.y, dragStartWorld.x - pivot.x) * Mathf.Rad2Deg;
            var a1 = Mathf.Atan2(currentWorld.y - pivot.y, currentWorld.x - pivot.x) * Mathf.Rad2Deg;
            var delta = Mathf.DeltaAngle(a0, a1);
            Undo.RecordObject(target, "Rotate");
            target.rotation = Quaternion.Euler(0f, 0f, initialRotationZ + delta);
        }

        // ==================== Favorites + PaintTarget persistence ====================

        private void LoadFavorites()
        {
            var raw = EditorPrefs.GetString(FavoritesPrefKey, "");
            PaletteCommon.favoriteGuids.Clear();
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var g in raw.Split(';'))
                if (!string.IsNullOrEmpty(g)) PaletteCommon.favoriteGuids.Add(g);
        }

        private void SavePaintTarget()
        {
            if (PaletteCommon.paintTarget == null)
            {
                EditorPrefs.DeleteKey(PaintTargetPrefKey);
                return;
            }
            var id = GlobalObjectId.GetGlobalObjectIdSlow(PaletteCommon.paintTarget.gameObject).ToString();
            EditorPrefs.SetString(PaintTargetPrefKey, id);
        }

        private void RestorePaintTarget()
        {
            var idString = EditorPrefs.GetString(PaintTargetPrefKey, "");
            if (string.IsNullOrEmpty(idString)) return;
            if (!GlobalObjectId.TryParse(idString, out var id)) return;
            var go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
            if (go != null) PaletteCommon.paintTarget = go.transform;
        }

        // ==================== Status bar text ====================

        private string BuildStatusText()
        {
            var cursor = PaletteCommon.lastCursorWorld;
            var entry = PaletteCommon.selection.IsEmpty
                ? "(no selection)"
                : PaletteCommon.selection.selection[0]?.name ?? "?";
            var target = PaletteCommon.paintTarget != null
                ? GetHierarchyPath(PaletteCommon.paintTarget)
                : "(unset)";
            return $"cursor ({cursor.x:F1}, {cursor.y:F1})   entry: {entry}   under: {target}   mode: {PaletteCommon.mode}";
        }

        static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";
            var s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }
    }
}
