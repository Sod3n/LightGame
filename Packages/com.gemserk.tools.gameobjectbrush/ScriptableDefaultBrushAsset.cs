using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
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
                // DontSave without NotEditable: excludes this from scene serialization
                // independently of the parent's flags (don't rely on save-exclusion inheriting
                // down the hierarchy), while keeping it Inspector-editable — users are meant to
                // be able to tweak the preview's values before painting (see Paint()'s
                // per-instance override copy).
                preview.hideFlags = HideFlags.DontSave;
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

                    // Mirror per-component values from the preview onto the new instance so
                    // Inspector edits made on the preview (before clicking) carry over as
                    // prefab-instance overrides.
                    CopyOverridesRecursive(previewInstance.transform, paintedObject.transform,
                        previewInstance.transform, paintedObject.transform);
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
                Editor.PaletteCommon.lastPaintedGameObject = paintedObject;
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

        // Copies each non-Transform component's serialized data from source to destination,
        // field by field, via SerializedObject/SerializedProperty rather than Unity's clipboard
        // flow (ComponentUtility.CopyComponent + PasteComponentValues). The clipboard flow copies
        // object-reference fields as literal references — if a script on the prefab references
        // something INSIDE its own hierarchy (e.g. a controller pointing at a child hitbox), a
        // blind paste would overwrite the freshly-instantiated object's correct self-reference
        // with a pointer into the (soon destroyed/regenerated) preview hierarchy instead of its
        // own child. References that point outside src's hierarchy (other scene objects, assets)
        // are left untouched, preserving intentional overrides.
        // Assumes src and dst hierarchies match (both freshly instantiated from the same prefab).
        // Skips the root Transform because the caller writes it.
        private static void CopyOverridesRecursive(Transform src, Transform dst, Transform srcRoot, Transform dstRoot)
        {
            var isRoot = src == srcRoot;
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
                CopySerializedValuesRemappingInternalRefs(s, d, srcRoot, dstRoot);
            }
            int childCount = Mathf.Min(src.childCount, dst.childCount);
            for (int i = 0; i < childCount; i++)
                CopyOverridesRecursive(src.GetChild(i), dst.GetChild(i), srcRoot, dstRoot);
        }

        private static void CopySerializedValuesRemappingInternalRefs(Component s, Component d, Transform srcRoot, Transform dstRoot)
        {
            var srcSO = new SerializedObject(s);
            var dstSO = new SerializedObject(d);
            var iterator = srcSO.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.propertyType == SerializedPropertyType.Generic) continue; // container node — its children are visited individually
                if (iterator.propertyPath == "m_Script") continue;

                var dstProp = dstSO.FindProperty(iterator.propertyPath);
                if (dstProp == null) continue;

                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    dstProp.objectReferenceValue = RemapIfInternal(iterator.objectReferenceValue, srcRoot, dstRoot);
                else
                    CopyLeafValue(iterator, dstProp);
            }
            dstSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // SerializedProperty has no generic "copy value from another property" API — each
        // leaf type needs its own typed accessor.
        private static void CopyLeafValue(SerializedProperty src, SerializedProperty dst)
        {
            switch (src.propertyType)
            {
                case SerializedPropertyType.Integer: dst.longValue = src.longValue; break;
                case SerializedPropertyType.Boolean: dst.boolValue = src.boolValue; break;
                case SerializedPropertyType.Float: dst.doubleValue = src.doubleValue; break;
                case SerializedPropertyType.String: dst.stringValue = src.stringValue; break;
                case SerializedPropertyType.Color: dst.colorValue = src.colorValue; break;
                case SerializedPropertyType.LayerMask: dst.intValue = src.intValue; break;
                case SerializedPropertyType.Enum: dst.enumValueIndex = src.enumValueIndex; break;
                case SerializedPropertyType.Vector2: dst.vector2Value = src.vector2Value; break;
                case SerializedPropertyType.Vector3: dst.vector3Value = src.vector3Value; break;
                case SerializedPropertyType.Vector4: dst.vector4Value = src.vector4Value; break;
                case SerializedPropertyType.Rect: dst.rectValue = src.rectValue; break;
                case SerializedPropertyType.ArraySize: dst.intValue = src.intValue; break; // resizes dst's array to match
                case SerializedPropertyType.Character: dst.intValue = src.intValue; break;
                case SerializedPropertyType.AnimationCurve: dst.animationCurveValue = src.animationCurveValue; break;
                case SerializedPropertyType.Bounds: dst.boundsValue = src.boundsValue; break;
                case SerializedPropertyType.Quaternion: dst.quaternionValue = src.quaternionValue; break;
                case SerializedPropertyType.ExposedReference: dst.exposedReferenceValue = src.exposedReferenceValue; break;
                case SerializedPropertyType.Vector2Int: dst.vector2IntValue = src.vector2IntValue; break;
                case SerializedPropertyType.Vector3Int: dst.vector3IntValue = src.vector3IntValue; break;
                case SerializedPropertyType.RectInt: dst.rectIntValue = src.rectIntValue; break;
                case SerializedPropertyType.BoundsInt: dst.boundsIntValue = src.boundsIntValue; break;
                case SerializedPropertyType.ManagedReference: dst.managedReferenceValue = src.managedReferenceValue; break;
                case SerializedPropertyType.Hash128: dst.hash128Value = src.hash128Value; break;
                // Gradient has no public SerializedProperty accessor — left as-is on dst.
                default: break;
            }
        }

        // If `value` is a GameObject/Component that lives inside srcRoot's hierarchy, returns the
        // corresponding object inside dstRoot's hierarchy (same sibling-index path down, same
        // same-type occurrence index on that GameObject). Anything else (assets, other scene
        // objects) is returned unchanged.
        private static Object RemapIfInternal(Object value, Transform srcRoot, Transform dstRoot)
        {
            if (value == null) return null;
            var owner = value is GameObject go ? go.transform : (value as Component)?.transform;
            if (owner == null || !IsDescendantOrSelf(owner, srcRoot)) return value;

            var indices = new List<int>();
            for (var t = owner; t != srcRoot; t = t.parent)
                indices.Add(t.GetSiblingIndex());
            indices.Reverse();

            var dstOwner = dstRoot;
            foreach (var idx in indices)
            {
                if (dstOwner == null || idx >= dstOwner.childCount) return value;
                dstOwner = dstOwner.GetChild(idx);
            }

            if (value is GameObject) return dstOwner.gameObject;
            if (value is Transform) return dstOwner;

            var comp = (Component)value;
            var srcOfType = owner.GetComponents(comp.GetType());
            var occurrence = System.Array.IndexOf(srcOfType, comp);
            var dstOfType = dstOwner.GetComponents(comp.GetType());
            return occurrence >= 0 && occurrence < dstOfType.Length ? dstOfType[occurrence] : value;
        }

        private static bool IsDescendantOrSelf(Transform t, Transform root)
        {
            for (; t != null; t = t.parent)
                if (t == root) return true;
            return false;
        }
#endif
    }
}
