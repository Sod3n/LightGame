using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Gemserk.Tools.ObjectPalette
{
    [CreateAssetMenu(menuName = "Object Palette/Modifiers/Multiply")]
    public class MultiplyModifier : BrushModifierAsset
    {
        public int min, max;

        public override void ApplyModifier(ScriptableBrushBaseAsset brush)
        {
            if (min <= 0 || max <= 1)
                return;

            var previewInstances = brush.previewInstances;

            foreach (var previewInstance in previewInstances)
            {
                var count = Random.Range(min, max);
                for (var i = 0; i < count; i++)
                {
#if UNITY_EDITOR
                    var prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(previewInstance);
                    GameObject extra;
                    if (prefabRoot != null)
                    {
                        // Fresh prefab instance, then paste per-component values from the
                        // (user-edited) preview so extras match the template's overrides.
                        extra = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot);
                        extra.transform.SetParent(previewInstance.transform.parent, worldPositionStays: false);
                        MirrorComponentValues(previewInstance.transform, extra.transform, isRoot: true);
                    }
                    else
                    {
                        extra = Instantiate(previewInstance, previewInstance.transform.parent);
                    }
#else
                    Instantiate(previewInstance, previewInstance.transform.parent);
#endif
                }
            }
        }

#if UNITY_EDITOR
        private static void MirrorComponentValues(Transform src, Transform dst, bool isRoot)
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
                MirrorComponentValues(src.GetChild(i), dst.GetChild(i), isRoot: false);
        }
#endif
    }
}
