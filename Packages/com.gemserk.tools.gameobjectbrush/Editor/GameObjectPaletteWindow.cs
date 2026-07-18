using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    public class GameObjectPaletteWindow : EditorWindow
    {
        public class SelectedPalette
        {
            public List<PaletteObject> cachedEntries;
        }

        [MenuItem("Window/Object Palette/Palette Window")]
        public static void OpenWindow()
        {
            var window = GetWindow(typeof(GameObjectPaletteWindow), false, "Object Palette");
            window.minSize = new Vector2(300, 300);
        }

        private List<ObjectPaletteBaseAsset> availablePalettes;
        private SelectedPalette _selectedSelectedPalette = new SelectedPalette();

        private List<ScriptableBrushBaseAsset> availableBrushes = new List<ScriptableBrushBaseAsset>();

        private Vector2 verticalScroll;
        private Tool previousTool;

        private int selectedBrushIndex;
        private int selectedPaletteIndex;

        public static bool windowVisible = false;

        private static readonly float buttonPreviewMinSize = 50;
        private static readonly float buttonPreviewMaxSize = 150;

        private float currentButtonSize;

        // Layer visibility panel: which sibling roots (peers of paintTarget) are hidden.
        private bool layersFoldout = true;
        private bool hotkeysFoldout = false;

        // Which per-category foldouts are open (keyed by category label). Session-scope.
        private readonly System.Collections.Generic.Dictionary<string, bool> categoryFoldouts
            = new System.Collections.Generic.Dictionary<string, bool>();

        [SerializeField]
        private ScriptableBrushBaseAsset defaultBrush = null;

        private const string PaintTargetPrefKey = "Gemserk.ObjectPalette.PaintTargetId";

        private void OnEnable()
        {
            ReloadPalettesAndBrushes();

            EditorSceneManager.sceneOpened += OnSceneOpened;

            DestroyHangingPreview();

            if (PaletteCommon.brush == null)
                PaletteCommon.brush = defaultBrush;

            RestorePaintTarget();
            LoadFavorites();
        }

        private void DestroyHangingPreview()
        {
            var scenes = EditorSceneManager.sceneCount;
            for (var i = 0; i < scenes; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObject in rootObjects)
                {
                    var hangingPreview = rootObject.GetComponentsInChildren<BrushPreview>();
                    foreach (var hangingBrush in hangingPreview)
                    {
                        DestroyImmediate(hangingBrush.gameObject);
                    }
                }
            }
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            PaletteCommon.brush?.DestroyPreview();
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single)
                UnselectPalette();
            RestorePaintTarget();
        }

        private void OnSceneViewGui(SceneView sceneView)
        {
            if (PalettePaintTool.HandlePaintHotkeys(Event.current))
            {
                sceneView.Repaint();
                Repaint();
                return;
            }

            // Alt+LMB: two flavors depending on what's under the cursor.
            //   - hovering an existing scene object → clone that object at cursor (Figma-style)
            //   - hovering empty space → duplicate last painted from palette
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

            // RMB drag-out to rotate the just-painted object. If the RMB gesture is a click
            // (no drag), fall through to the deselect handler further down.
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
                        PaletteCommon.brush.CreatePreview(PaletteCommon.selection);
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

            // RMB click (no drag) — deselect palette. `rmbWasDrag` set true in
            // HandleRmbRotateDrag suppresses this when the user rotated instead.
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

        // ================ Alt+drag existing scene object to clone ================

        // Clones an existing scene GameObject at the cursor. If the source is a prefab
        // instance, uses PrefabUtility to keep the prefab connection AND overrides.
        // Otherwise a plain Object.Instantiate copy.
        public static GameObject CloneExistingAt(GameObject source, Vector3 worldPos)
        {
            if (source == null) return null;
            GameObject clone;
            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(source);
            if (prefabAsset != null)
            {
                clone = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, source.scene);
                clone.transform.SetParent(source.transform.parent, worldPositionStays: false);
                // Copy overrides so the clone visually matches the source.
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

        // ================ Drag-out to rotate (RMB) ================

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
                    // Don't Use yet — click without drag should still fall through to deselect.
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

        // Testable: given the drag start and current world positions, rotate `target`
        // around its own origin so its Z rotation reflects the swept angle plus the initial rotation.
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

        private void OnBecameVisible()
        {
            windowVisible = true;

            SceneView.beforeSceneGui += OnBeforeSceneGui;
            SceneView.duringSceneGui += OnSceneViewGui;

            if (!PaletteCommon.selection.IsEmpty)
                PaletteCommon.brush?.CreatePreview(PaletteCommon.selection.selection);
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

        private void OnBecameInvisible()
        {
            windowVisible = false;

            SceneView.duringSceneGui -= OnSceneViewGui;
            SceneView.beforeSceneGui -= OnBeforeSceneGui;
            PaletteCommon.brush?.DestroyPreview();
        }

        private void OnFocus()
        {
            ReloadPalettesAndBrushes();
        }

        private void ReloadPalettesAndBrushes()
        {
            availablePalettes = AssetDatabaseExt.FindAssets<ObjectPaletteBaseAsset>();
            availableBrushes = AssetDatabaseExt.FindAssets<ScriptableBrushBaseAsset>();
        }

        private void ReloadSelectedPalette()
        {
            var palette = availablePalettes[selectedPaletteIndex];
            _selectedSelectedPalette.cachedEntries = palette.CreatePaletteObjects();
        }

        private void OnGUI()
        {
            if (Event.current.rawType == EventType.KeyUp && Event.current.keyCode == KeyCode.Escape)
                UnselectPalette();

            DrawBrushList();
            DrawPaletteList();
            DrawPaintTarget();
            DrawTransformOffsets();
            DrawLayerVisibility();
            DrawFavoritesAndRecents();
            DrawHotkeyHints();

            if (availablePalettes.Count == 0)
            {
                DrawStatusBar();
                return;
            }

            ReloadSelectedPalette();

            var buttonSize = new Vector2(currentButtonSize, currentButtonSize);

            GUILayout.BeginVertical();

            verticalScroll = GUILayout.BeginScrollView(verticalScroll, false, true,
                GUIStyle.none, GUI.skin.verticalScrollbar);

            var fontStyle = new GUIStyle(GUI.skin.GetStyle("PreOverlayLabel"))
            {
                fontSize = 10
            };

            var multiselection = Event.current.shift;

            // Group entries by leaf-folder name (e.g., "Tiles", "Platforms"). Folder → list.
            var groups = GroupByCategory(_selectedSelectedPalette.cachedEntries);
            foreach (var kv in groups)
            {
                var category = kv.Key;
                var entries = kv.Value;
                if (!categoryFoldouts.TryGetValue(category, out var open)) open = true;
                open = EditorGUILayout.Foldout(open, $"{category}  ({entries.Count})", true);
                categoryFoldouts[category] = open;
                if (!open) continue;

                GUILayout.BeginHorizontal();
                var current = 0f;

                foreach (var entry in entries)
                {
                    if (entry == null) continue;

                    var previewSize = buttonSize;
                    var previewContent = new GUIContent { text = entry.name };
                    var guiStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        imagePosition = ImagePosition.ImageAbove,
                        fixedWidth = previewSize.x,
                        fixedHeight = previewSize.y
                    };

                    var isSelected = PaletteCommon.selection.Contains(entry);

                    if (GUILayout.Button(previewContent, guiStyle))
                    {
                        if (multiselection) SelectBrushObject(entry);
                        else if (isSelected) UnselectPalette();
                        else { UnselectPalette(); SelectBrushObject(entry); }
                    }

                    var r = GUILayoutUtility.GetLastRect();

                    if (entry.preview == null)
                        entry.preview = AssetPreview.GetAssetPreview(entry.sourceObject);
                    if (entry.preview != null) GUI.DrawTexture(r, entry.preview, ScaleMode.StretchToFill);
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, r.height), entry.name, fontStyle);
                    if (isSelected) EditorGUI.DrawRect(r, new Color(0, 0, 0.5f, 0.15f));

                    // Right-click on palette entry toggles favorite (pin/unpin).
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1
                        && r.Contains(Event.current.mousePosition))
                    {
                        ToggleFavorite(entry);
                        Event.current.Use();
                    }

                    current += buttonSize.x;
                    if (current >= position.width - buttonSize.x)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        current = 0;
                    }
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            GUILayout.EndScrollView();

            currentButtonSize = EditorGUILayout.Slider("Preview Size", currentButtonSize,
                buttonPreviewMinSize, buttonPreviewMaxSize);

            GUILayout.EndVertical();

            DrawStatusBar();
        }

        private void DrawPaletteList()
        {
            GUILayout.BeginVertical();

            if (availablePalettes.Count > 0)
            {
                var options = new List<string>();
                options.AddRange(availablePalettes.Select(b => b.name));

                EditorGUI.BeginChangeCheck();
                selectedPaletteIndex = EditorGUILayout.Popup("Palette", selectedPaletteIndex, options.ToArray());

                if (EditorGUI.EndChangeCheck())
                    _selectedSelectedPalette.cachedEntries = null;
            }
            else
            {
                GUILayout.Label("No palettes found");
            }

            GUILayout.EndVertical();
        }

        private void DrawBrushList()
        {
            GUILayout.BeginVertical();

            if (availableBrushes.Count > 0)
            {
                var options = new List<string>();
                options.AddRange(availableBrushes.Select(b => b.name));

                selectedBrushIndex = availableBrushes.IndexOf(PaletteCommon.brush as ScriptableBrushBaseAsset);

                EditorGUI.BeginChangeCheck();
                selectedBrushIndex = EditorGUILayout.Popup("Brush", selectedBrushIndex, options.ToArray());

                if (EditorGUI.EndChangeCheck())
                    PaletteCommon.brush = availableBrushes[selectedBrushIndex];
            }

            GUILayout.EndVertical();
        }

        private void DrawPaintTarget()
        {
            EditorGUI.BeginChangeCheck();
            var picked = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Paint Under", "New painted objects become children of this transform. Falls back to Selection if empty."),
                PaletteCommon.paintTarget, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck())
            {
                PaletteCommon.paintTarget = picked;
                SavePaintTarget();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selection", GUILayout.Width(120)))
                {
                    var sel = Selection.activeTransform;
                    if (sel != null && sel.gameObject.scene.IsValid())
                    {
                        PaletteCommon.paintTarget = sel;
                        SavePaintTarget();
                    }
                }
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                {
                    PaletteCommon.paintTarget = null;
                    SavePaintTarget();
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawTransformOffsets()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Drag spacing",
                    "World-space distance the cursor must travel during a paint-drag before another paint fires. 0 = every drag paints."),
                    GUILayout.Width(90));
                var newSpacing = EditorGUILayout.FloatField(PaletteCommon.dragSpacing, GUILayout.Width(60));
                PaletteCommon.dragSpacing = Mathf.Max(0f, newSpacing);
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Rot / Scale", "Hotkeys in scene view:  [ ]  rotate 15°   - =  scale 10%   0  reset"),
                    GUILayout.Width(80));
                EditorGUI.BeginChangeCheck();
                var rot = EditorGUILayout.FloatField(PaletteCommon.paintRotationDegrees, GUILayout.Width(60));
                var scl = EditorGUILayout.FloatField(PaletteCommon.paintScaleMultiplier, GUILayout.Width(60));
                if (EditorGUI.EndChangeCheck())
                {
                    PaletteCommon.paintRotationDegrees = rot;
                    PaletteCommon.paintScaleMultiplier = Mathf.Max(0.01f, scl);
                    (PaletteCommon.brush as ScriptableBrushBaseAsset)?.ApplyPreviewTransform();
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Reset", GUILayout.Width(60)))
                {
                    PaletteCommon.paintRotationDegrees = 0f;
                    PaletteCommon.paintScaleMultiplier = 1f;
                    (PaletteCommon.brush as ScriptableBrushBaseAsset)?.ApplyPreviewTransform();
                    SceneView.RepaintAll();
                }
            }
        }

        private void DrawLayerVisibility()
        {
            var target = PaletteCommon.paintTarget;
            if (target == null) return;
            var parent = target.parent;
            var siblings = parent == null
                ? target.gameObject.scene.GetRootGameObjects().Select(g => g.transform).ToList()
                : Enumerable.Range(0, parent.childCount).Select(i => parent.GetChild(i)).ToList();

            if (siblings.Count <= 1) return;

            layersFoldout = EditorGUILayout.Foldout(layersFoldout, "Layer Visibility", true);
            if (!layersFoldout) return;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var s in siblings)
                {
                    if (s == null) continue;
                    var go = s.gameObject;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var isTarget = s == target;
                        EditorGUI.BeginChangeCheck();
                        var active = EditorGUILayout.ToggleLeft(
                            (isTarget ? "► " : "   ") + go.name, go.activeSelf);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(go, "Toggle Layer Visibility");
                            go.SetActive(active);
                            EditorSceneManager.MarkSceneDirty(go.scene);
                        }
                        if (!isTarget && GUILayout.Button("Focus", GUILayout.Width(60)))
                        {
                            PaletteCommon.paintTarget = s;
                            SavePaintTarget();
                        }
                    }
                }
            }
        }

        // ================ Status bar ================

        private void DrawStatusBar()
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10
            };
            var cursor = PaletteCommon.lastCursorWorld;
            var entryName = PaletteCommon.selection.IsEmpty
                ? "(no selection)"
                : PaletteCommon.selection.selection[0]?.name ?? "?";
            var targetPath = PaletteCommon.paintTarget != null
                ? GetHierarchyPath(PaletteCommon.paintTarget)
                : "(paint target unset)";
            var line = $"cursor: ({cursor.x:F1}, {cursor.y:F1})    entry: {entryName}    under: {targetPath}    mode: {PaletteCommon.mode}";

            var rect = GUILayoutUtility.GetRect(new GUIContent(line), style,
                GUILayout.Height(16), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.10f));
            GUI.Label(rect, "  " + line, style);
        }

        static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";
            var s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        // Groups palette entries by their leaf-folder name (from AssetDatabase path). Entries
        // without an asset path or in the top-level project fall under "General". Preserves
        // the original order within each group. Returned as insertion-ordered list of groups.
        public static System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<PaletteObject>>>
            GroupByCategory(System.Collections.Generic.IList<PaletteObject> entries)
        {
            var order = new System.Collections.Generic.List<string>();
            var buckets = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<PaletteObject>>();
            if (entries == null)
                return new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<PaletteObject>>>();
            foreach (var e in entries)
            {
                if (e == null) continue;
                var cat = CategoryFor(e);
                if (!buckets.ContainsKey(cat))
                {
                    buckets[cat] = new System.Collections.Generic.List<PaletteObject>();
                    order.Add(cat);
                }
                buckets[cat].Add(e);
            }
            var result = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<PaletteObject>>>();
            foreach (var cat in order)
                result.Add(new System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<PaletteObject>>(cat, buckets[cat]));
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

        // ================ Recent + favorites strip ================

        private const string FavoritesPrefKey = "Gemserk.ObjectPalette.Favorites";

        private void LoadFavorites()
        {
            var raw = EditorPrefs.GetString(FavoritesPrefKey, "");
            PaletteCommon.favoriteGuids.Clear();
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var g in raw.Split(';'))
                if (!string.IsNullOrEmpty(g)) PaletteCommon.favoriteGuids.Add(g);
        }

        private void SaveFavorites()
        {
            EditorPrefs.SetString(FavoritesPrefKey, string.Join(";", PaletteCommon.favoriteGuids));
        }

        // Toggle a palette entry's pinned/favorite state. Public + static so tests can
        // exercise it without needing a live window. Persists via EditorPrefs.
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

        private void DrawFavoritesAndRecents()
        {
            if (PaletteCommon.recentEntries.Count == 0 && PaletteCommon.favoriteGuids.Count == 0)
                return;

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Quick", GUILayout.Width(40));
                var size = new Vector2(32, 32);
                var drawn = new HashSet<Object>();

                // Favorites first
                if (_selectedSelectedPalette.cachedEntries != null)
                {
                    foreach (var e in _selectedSelectedPalette.cachedEntries)
                    {
                        if (e == null) continue;
                        if (!IsFavorite(e)) continue;
                        if (DrawQuickButton(e, size, isFavorite: true)) SelectBrushObject(e);
                        drawn.Add(e.sourceObject);
                    }
                }
                // Then recents (skipping any already shown as favorite)
                foreach (var e in PaletteCommon.recentEntries)
                {
                    if (e == null || drawn.Contains(e.sourceObject)) continue;
                    if (DrawQuickButton(e, size, isFavorite: false)) SelectBrushObject(e);
                }
                GUILayout.FlexibleSpace();
            }
        }

        static bool DrawQuickButton(PaletteObject entry, Vector2 size, bool isFavorite)
        {
            var content = new GUIContent(isFavorite ? "★ " + entry.name : entry.name, entry.name);
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = size.x, fixedHeight = size.y,
                imagePosition = ImagePosition.ImageOnly
            };
            var clicked = GUILayout.Button(content, style);
            var rect = GUILayoutUtility.GetLastRect();
            if (entry.preview != null) GUI.DrawTexture(rect, entry.preview, ScaleMode.StretchToFill);
            if (isFavorite)
            {
                var star = new Rect(rect.x + 1, rect.y + 1, 10, 10);
                GUI.Label(star, "★");
            }
            // Right-click to toggle favorite
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 1
                && rect.Contains(Event.current.mousePosition))
            {
                ToggleFavorite(entry);
                Event.current.Use();
            }
            return clicked;
        }

        private void DrawHotkeyHints()
        {
            hotkeysFoldout = EditorGUILayout.Foldout(hotkeysFoldout, "Hotkeys (rebind in Edit > Shortcuts)", true);
            if (!hotkeysFoldout) return;
            using (new EditorGUI.IndentLevelScope())
            {
                var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                EditorGUILayout.LabelField("LMB              — paint (MMB reserved for camera pan)", style);
                EditorGUILayout.LabelField("Alt + LMB        — duplicate last painted (bypasses palette)", style);
                EditorGUILayout.LabelField("[  ]             — rotate ±15°", style);
                EditorGUILayout.LabelField("-  =             — scale ±10%", style);
                EditorGUILayout.LabelField("0                — reset rotation & scale", style);
                EditorGUILayout.LabelField("E                — toggle erase mode", style);
                EditorGUILayout.LabelField("V                — exit paint mode, back to Move tool", style);
                EditorGUILayout.LabelField("Arrows           — nudge selected 1u  (Shift = 10u, Ctrl = 0.1u)", style);
                EditorGUILayout.LabelField("Esc / RMB        — deselect palette entry", style);
                EditorGUILayout.LabelField("Shift+click      — multi-select in palette", style);
                EditorGUILayout.LabelField("Ctrl+wheel       — regenerate preview", style);
            }
        }

        // Paints the remembered "last painted" palette entry once at world-space `pos`,
        // without permanently disturbing the current palette selection or preview state.
        public static void DuplicateLastPaintedAt(Vector3 pos)
        {
            var entry = PaletteCommon.lastPaintedEntry;
            var brush = PaletteCommon.brush as ScriptableBrushBaseAsset;
            if (entry == null || brush == null) return;

            var savedSelection = new System.Collections.Generic.List<PaletteObject>(PaletteCommon.selection.selection);

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

        private void UnselectPalette()
        {
            PaletteCommon.brush?.DestroyPreview();
            PaletteCommon.selection.Clear();
            RestoreUnityTool();
        }

        private void SelectBrushObject(PaletteObject o)
        {
            PaletteCommon.selection.Add(o);
            PaletteCommon.brush.CreatePreview(PaletteCommon.selection.selection);
            UnselectUnityTool();
            (PaletteCommon.brush as ScriptableBrushBaseAsset)?.ApplyPreviewTransform();

            // Show the preview instance in the inspector so user can add inline overrides
            // (color, sprite, scale). Those overrides carry forward into painted instances.
            var brush = PaletteCommon.brush as ScriptableBrushBaseAsset;
            if (brush?.previewParent != null && brush.previewParent.childCount > 0)
                Selection.activeGameObject = brush.previewParent.GetChild(0).gameObject;
            else if (o.sourceObject != null)
                Selection.activeObject = o.sourceObject;
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
    }
}
