using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    // Scene-view overlay showing the palette's favorites + recents as a compact strip.
    // Toggle via Scene View toolbar > Overlays > "Palette Quick".
    // Dockable, collapsible, remembers position per-user.
    [Overlay(typeof(SceneView), OverlayId, "Palette Quick", true)]
    [Icon("d_Prefab Icon")]
    public class PaletteQuickOverlay : Overlay
    {
        private const string OverlayId = "gemserk.object-palette.quick";

        private VisualElement root;

        public override void OnCreated()
        {
            base.OnCreated();
            PaletteCommon.onQuickChanged += Rebuild;
        }

        public override void OnWillBeDestroyed()
        {
            PaletteCommon.onQuickChanged -= Rebuild;
            base.OnWillBeDestroyed();
        }

        public override VisualElement CreatePanelContent()
        {
            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            root.style.maxWidth = 260;
            Rebuild();
            return root;
        }

        private void Rebuild()
        {
            if (root == null) return;
            root.Clear();

            var toggleRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 3 } };
            toggleRow.Add(BuildEraseToggle());
            toggleRow.Add(BuildSelectModeToggle());
            toggleRow.Add(BuildFocusModeToggle());
            root.Add(toggleRow);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            root.Add(grid);

            var seen = new HashSet<Object>();

            // Favorites first (starred)
            foreach (var g in PaletteCommon.favoriteGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null || seen.Contains(go)) continue;
                seen.Add(go);
                var entry = new PaletteObject
                {
                    name = go.name,
                    sourceObject = go,
                    preview = AssetPreview.GetAssetPreview(go)
                };
                grid.Add(BuildBtn(entry, favorite: true));
            }

            // Recents
            foreach (var e in PaletteCommon.recentEntries)
            {
                if (e?.sourceObject == null || seen.Contains(e.sourceObject)) continue;
                seen.Add(e.sourceObject);
                if (e.preview == null) e.preview = AssetPreview.GetAssetPreview(e.sourceObject);
                grid.Add(BuildBtn(e, favorite: false));
            }

            if (grid.childCount == 0)
            {
                var empty = new Label("Paint or pin a palette entry to populate.")
                {
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Italic,
                        fontSize = 10,
                        color = new Color(0.7f, 0.7f, 0.7f),
                        maxWidth = 240,
                        whiteSpace = WhiteSpace.Normal
                    }
                };
                grid.Add(empty);
            }
        }

        private VisualElement BuildEraseToggle()
        {
            var erasing = PaletteCommon.mode == PaletteToolMode.Erase;
            var btn = new Button(() => PaletteCommon.SetMode(erasing ? PaletteToolMode.Paint : PaletteToolMode.Erase))
            {
                text = erasing ? "Erase: On (E)" : "Erase (E)",
                tooltip = "Toggle erase mode (E). Erases only objects under the current Paint Target."
            };
            StyleModeButton(btn, erasing, new Color(0.85f, 0.3f, 0.3f));
            btn.style.marginRight = 3;
            return btn;
        }

        private VisualElement BuildSelectModeToggle()
        {
            var on = GameObjectPaletteWindow.selectModeActive;
            var btn = new Button(() =>
            {
                if (GameObjectPaletteWindow.selectModeActive) GameObjectPaletteWindow.ExitSelectMode();
                else GameObjectPaletteWindow.EnterSelectMode();
            })
            {
                text = on ? "Select: On (4)" : "Select (4)",
                tooltip = "Edit existing objects (Move gizmo, Inspector) without changing Paint Target (4). " +
                          "Combine with Focus to scope editing to just the current layer."
            };
            StyleModeButton(btn, on, new Color(0.6f, 0.6f, 0.6f));
            btn.style.marginLeft = 3;
            btn.style.marginRight = 3;
            return btn;
        }

        private VisualElement BuildFocusModeToggle()
        {
            var on = PaletteFocusMode.Enabled;
            var btn = new Button(() =>
            {
                PaletteFocusMode.SetEnabled(!PaletteFocusMode.Enabled);
                PaletteCommon.RaiseQuickChanged();
            })
            {
                text = on ? "Focus: On (3)" : "Focus (3)",
                tooltip = "Highlight the Paint Target's objects and hide + disable clicking on everything else " +
                          "(non-destructive — Scene Visibility only, nothing else is selectable while this is on). Hotkey: 3."
            };
            StyleModeButton(btn, on, new Color(0.3f, 0.75f, 1f));
            return btn;
        }

        private static void StyleModeButton(Button btn, bool active, Color activeColor)
        {
            btn.style.height = 20;
            btn.style.fontSize = 10;
            btn.style.flexGrow = 1;
            if (active)
            {
                btn.style.backgroundColor = activeColor;
                btn.style.color = Color.white;
            }
        }

        private VisualElement BuildBtn(PaletteObject entry, bool favorite)
        {
            var btn = new Button { tooltip = favorite ? "★ " + entry.name : entry.name };
            btn.style.width = 36;
            btn.style.height = 36;
            btn.style.marginRight = 1;
            btn.style.marginBottom = 1;
            btn.style.paddingLeft = 0; btn.style.paddingRight = 0;
            btn.style.paddingTop = 0; btn.style.paddingBottom = 0;

            btn.clicked += () => GameObjectPaletteWindow.SelectBrushObjectStatic(entry);

            var preview = entry.preview;
            if (preview != null) btn.style.backgroundImage = new StyleBackground((Texture2D)preview);

            // Selection highlight (blue outline).
            if (!PaletteCommon.selection.IsEmpty
                && ReferenceEquals(PaletteCommon.selection.selection[0]?.sourceObject, entry.sourceObject))
            {
                var blue = new Color(0.4f, 0.7f, 1f);
                btn.style.borderTopWidth = 2; btn.style.borderBottomWidth = 2;
                btn.style.borderLeftWidth = 2; btn.style.borderRightWidth = 2;
                btn.style.borderTopColor = blue; btn.style.borderBottomColor = blue;
                btn.style.borderLeftColor = blue; btn.style.borderRightColor = blue;
            }

            if (favorite)
            {
                var star = new Label("★")
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        top = -3, left = 0,
                        color = new Color(1f, 0.9f, 0.2f),
                        fontSize = 14
                    }
                };
                btn.Add(star);
            }

            // Right-click via context menu — cleaner than intercepting MouseDown
            // (which conflicted with Button's own Clickable manipulator).
            btn.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction(favorite ? "Unpin favorite" : "Pin as favorite",
                    _ => GameObjectPaletteWindow.ToggleFavorite(entry));
            }));

            return btn;
        }
    }
}
