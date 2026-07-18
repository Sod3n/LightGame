using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    // Scene-view overlay showing the palette's favorites + recents as a compact strip.
    // Toggle via Scene View toolbar > Overlays > "Palette Quick" (or the "⋯" menu).
    // Dockable, collapsible, remembers its position per-user like any Overlay.
    [Overlay(typeof(SceneView), OverlayId, "Palette Quick", true)]
    [Icon("d_Prefab Icon")]
    public class PaletteQuickOverlay : Overlay
    {
        private const string OverlayId = "gemserk.object-palette.quick";

        private VisualElement root;
        private List<Object> lastSignature;

        public override VisualElement CreatePanelContent()
        {
            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexWrap = Wrap.Wrap;
            root.style.maxWidth = 240;

            Rebuild();
            EditorApplication.update += Tick;
            return root;
        }

        public override void OnWillBeDestroyed()
        {
            EditorApplication.update -= Tick;
            base.OnWillBeDestroyed();
        }

        // Cheap dirty-check every editor tick so the overlay stays in sync when the palette
        // window (or a paint action) adds to recents or the user pins/unpins favorites.
        private void Tick()
        {
            var sig = BuildSignature();
            if (lastSignature != null && SignatureEquals(sig, lastSignature)) return;
            lastSignature = sig;
            Rebuild();
        }

        private List<Object> BuildSignature()
        {
            var sig = new List<Object>();
            foreach (var e in PaletteCommon.recentEntries) if (e?.sourceObject != null) sig.Add(e.sourceObject);
            foreach (var g in PaletteCommon.favoriteGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (obj != null) sig.Add(obj);
            }
            var selName = PaletteCommon.selection.IsEmpty ? "" : PaletteCommon.selection.selection[0]?.name ?? "";
            sig.Add(new UnityEngine.TextAsset(selName)); // pseudo-marker for selection highlight
            return sig;
        }

        private static bool SignatureEquals(List<Object> a, List<Object> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!ReferenceEquals(a[i], b[i])) return false;
            return true;
        }

        private void Rebuild()
        {
            if (root == null) return;
            root.Clear();

            var seen = new HashSet<Object>();

            // Favorites first (starred)
            foreach (var g in PaletteCommon.favoriteGuids.ToList())
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null || seen.Contains(go)) continue;
                seen.Add(go);
                var entry = new PaletteObject { name = go.name, sourceObject = go };
                root.Add(BuildBtn(entry, favorite: true));
            }

            // Recents (excluding anything already shown as favorite)
            foreach (var e in PaletteCommon.recentEntries)
            {
                if (e?.sourceObject == null || seen.Contains(e.sourceObject)) continue;
                seen.Add(e.sourceObject);
                root.Add(BuildBtn(e, favorite: false));
            }

            if (root.childCount == 0)
            {
                var empty = new Label("Paint or pin a palette entry to populate.")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Italic, fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f), maxWidth = 220, whiteSpace = WhiteSpace.Normal }
                };
                root.Add(empty);
            }
        }

        private VisualElement BuildBtn(PaletteObject entry, bool favorite)
        {
            var btn = new Button(() => GameObjectPaletteWindow.SelectBrushObjectStatic(entry)) { tooltip = favorite ? "★ " + entry.name : entry.name };
            btn.style.width = 32;
            btn.style.height = 32;
            btn.style.marginRight = 1;
            btn.style.marginBottom = 1;
            btn.style.paddingLeft = 0;
            btn.style.paddingRight = 0;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;

            var preview = entry.preview ?? AssetPreview.GetAssetPreview(entry.sourceObject);
            entry.preview = preview;
            if (preview != null) btn.style.backgroundImage = new StyleBackground((Texture2D)preview);

            // Selected highlight
            if (!PaletteCommon.selection.IsEmpty
                && ReferenceEquals(PaletteCommon.selection.selection[0]?.sourceObject, entry.sourceObject))
            {
                btn.style.borderTopWidth = 2; btn.style.borderBottomWidth = 2;
                btn.style.borderLeftWidth = 2; btn.style.borderRightWidth = 2;
                btn.style.borderTopColor = btn.style.borderBottomColor =
                    btn.style.borderLeftColor = btn.style.borderRightColor = new Color(0.4f, 0.7f, 1f);
            }

            if (favorite)
            {
                var star = new Label("★")
                {
                    style = { position = Position.Absolute, top = -2, left = 1,
                              color = new Color(1f, 0.9f, 0.2f), fontSize = 12 }
                };
                btn.Add(star);
            }

            // Right-click toggles favorite pin.
            btn.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    GameObjectPaletteWindow.ToggleFavorite(entry);
                    Rebuild();
                    evt.StopPropagation();
                }
            });
            return btn;
        }
    }
}
