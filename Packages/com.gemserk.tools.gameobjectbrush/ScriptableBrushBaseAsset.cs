using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gemserk.Tools.ObjectPalette
{
    public abstract class ScriptableBrushBaseAsset : ScriptableObject, IBrush
    {
        public bool regenerateOnPaint = false;

        public bool RegenerateOnPaint => regenerateOnPaint;

        [NonSerialized]
        public Vector2 position;

        public List<GameObject> previewInstances
        {
            get
            {
                var list = new List<GameObject>();
                for (var i = 0; i < previewParent.childCount; i++)
                    list.Add(previewParent.GetChild(i).gameObject);
                return list;
            }
        }

        [NonSerialized]
        public Transform previewParent;

        [SerializeField]
        protected List<BrushModifierAsset> modifiers = new List<BrushModifierAsset>();

        public virtual void UpdatePosition(Vector2 p)
        {
            position = p;
            if (previewParent != null)
                previewParent.position = p;
#if UNITY_EDITOR
            Editor.PaletteCommon.lastCursorWorld = p;
#endif
            foreach (var modifier in modifiers)
                modifier.UpdatePosition(this);
        }

        public abstract void CreatePreview(IEnumerable<PaletteObject> paletteObjects);

        protected void CreateParent()
        {
            if (previewParent != null)
                return;
            var brushPreviewObject = new GameObject("~BrushPreview")
            {
                hideFlags = HideFlags.NotEditable,
                tag = "EditorOnly"
            };

            brushPreviewObject.AddComponent<BrushPreview>();

            previewParent = brushPreviewObject.transform;
            previewParent.position = position;

#if UNITY_EDITOR
            // Put the preview holder in the same scene as the current paint target so nothing
            // ends up in random "Untitled" scenes when multiple are loaded.
            var target = Editor.PaletteCommon.paintTarget;
            if (target != null && target.gameObject.scene.IsValid())
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(brushPreviewObject, target.gameObject.scene);
#endif

            ApplyPreviewTransform();
        }

        // Applies the session-wide rotation/scale offsets (hotkeys) to the preview root.
        public void ApplyPreviewTransform()
        {
#if UNITY_EDITOR
            if (previewParent == null) return;
            previewParent.rotation = Quaternion.Euler(0, 0, Editor.PaletteCommon.paintRotationDegrees);
            previewParent.localScale = Vector3.one * Editor.PaletteCommon.paintScaleMultiplier;
#endif
        }

        public virtual void DestroyPreview()
        {
            if (previewParent != null)
                DestroyImmediate(previewParent.gameObject);
            previewParent = null;
        }

        public abstract void Paint();
    }
}
