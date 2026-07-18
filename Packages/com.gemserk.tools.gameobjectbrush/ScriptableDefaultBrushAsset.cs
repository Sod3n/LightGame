using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Gemserk.Tools.ObjectPalette
{
    [CreateAssetMenu(menuName = "Object Palette/Default Brush")]
    public class ScriptableDefaultBrushAsset : ScriptableBrushBaseAsset
    {
        public override void CreatePreview(IEnumerable<PaletteObject> paletteObjects)
        {
            DestroyPreview();
            CreateParent();

            foreach (var paletteObject in paletteObjects)
            {
#if UNITY_EDITOR
                var preview = paletteObject.Instantiate();
                // SetParent(worldPositionStays: false) keeps local rotation & scale as-authored
                // (so the prefab's baked local scale, e.g. Ground_scaled x=6, is preserved),
                // AND lets the parent's session rotation/scale offsets multiply through.
                // `preview.transform.parent = previewParent` would use worldPositionStays:true
                // which counters the parent's transform on the child and defeats the offsets.
                preview.transform.SetParent(previewParent, worldPositionStays: false);
                preview.transform.localPosition = Vector3.zero;
#endif
            }

            foreach (var modifier in modifiers)
                modifier.ApplyModifier(this);

            ApplyPreviewTransform();
        }

        public override void Paint()
        {
#if UNITY_EDITOR
            // Remember what we're painting so "Alt+click = duplicate last painted" works
            // even after the palette selection is cleared. Also push into the recent-used list.
            if (Editor.PaletteCommon.selection != null && Editor.PaletteCommon.selection.selection.Count > 0)
            {
                var entry = Editor.PaletteCommon.selection.selection[0];
                Editor.PaletteCommon.lastPaintedEntry = entry;
                Editor.PaletteCommon.RememberRecent(entry);
            }
#endif
            foreach (var previewInstance in previewInstances)
            {
#if UNITY_EDITOR
                var paintParent = ResolvePaintParent();
                var targetScene = paintParent != null
                    ? paintParent.gameObject.scene
                    : SceneManager.GetActiveScene();

                // Capture the preview's world pose BEFORE we start creating things (reparenting
                // etc. can change things under us if the preview hierarchy is touched).
                var worldPos = previewInstance.transform.position;
                var worldRot = previewInstance.transform.rotation;
                var worldScale = previewInstance.transform.lossyScale;

                var prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(previewInstance);

                GameObject paintedObject;
                if (prefabRoot != null)
                {
                    // Fresh prefab instance (keeps the prefab link so the painted object is
                    // "part of a prefab" like every other level object).
                    paintedObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot, targetScene);
                    if (paintParent != null)
                        paintedObject.transform.SetParent(paintParent, worldPositionStays: false);

                    // Mirror per-component values from the preview onto the new instance.
                    // ComponentUtility.PasteComponentValues goes through Unity's clipboard flow,
                    // which properly records the values as prefab-instance overrides (unlike
                    // EditorUtility.CopySerialized which is silently reverted by the prefab system).
                    CopyOverridesRecursive(previewInstance.transform, paintedObject.transform, isRoot: true);
                }
                else
                {
                    paintedObject = (GameObject)Object.Instantiate(previewInstance);
                    paintedObject.name = previewInstance.name;
                    paintedObject.hideFlags = HideFlags.None;
                    if (paintedObject.scene != targetScene && targetScene.IsValid())
                        SceneManager.MoveGameObjectToScene(paintedObject, targetScene);
                    if (paintParent != null)
                        paintedObject.transform.SetParent(paintParent, worldPositionStays: false);
                    else
                        paintedObject.transform.SetParent(null, worldPositionStays: false);
                }

                paintedObject.transform.position = worldPos;
                paintedObject.transform.rotation = worldRot;
                paintedObject.transform.localScale = worldScale;

                Undo.RegisterCreatedObjectUndo(paintedObject, "Painted");
#else
                var paintedObject = Instantiate(previewInstance, previewParent.parent);
                paintedObject.transform.position = previewInstance.transform.position;
#endif
            }
        }

#if UNITY_EDITOR
        private static Transform ResolvePaintParent()
        {
            var target = Editor.PaletteCommon.paintTarget;
            if (target != null && target.gameObject.scene.IsValid() && target.gameObject.scene.isLoaded)
                return target;
            return null;
        }

        // Copies each non-Transform component's serialized data from source to destination via
        // Unity's clipboard flow (CopyComponent + PasteComponentValues). This is the same path the
        // Inspector's "Paste Component Values" uses, and it correctly records the pasted values
        // as prefab-instance overrides. Assumes src and dst hierarchies match (both freshly
        // instantiated from the same prefab). Skips the root Transform because the caller writes it.
        private static void CopyOverridesRecursive(Transform src, Transform dst, bool isRoot)
        {
            var srcComps = src.GetComponents<Component>();
            var dstComps = dst.GetComponents<Component>();
            int n = Mathf.Min(srcComps.Length, dstComps.Length);
            for (int i = 0; i < n; i++)
            {
                var s = srcComps[i];
                var d = dstComps[i];
                if (s == null || d == null) continue;
                if (s.GetType() != d.GetType()) continue;
                if (s is Transform && isRoot) continue;
                ComponentUtility.CopyComponent(s);
                ComponentUtility.PasteComponentValues(d);
            }
            int childCount = Mathf.Min(src.childCount, dst.childCount);
            for (int i = 0; i < childCount; i++)
                CopyOverridesRecursive(src.GetChild(i), dst.GetChild(i), isRoot: false);
        }
#endif
    }
}
