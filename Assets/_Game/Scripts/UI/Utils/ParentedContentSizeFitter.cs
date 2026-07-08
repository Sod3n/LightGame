using UnityEngine;
using UnityEngine.UI;

namespace Light_and_controller.Scripts.UI.Utils
{
    public class ParentedContentSizeFitter : ContentSizeFitter
    {
        public override void SetLayoutHorizontal()
        {
            base.SetLayoutHorizontal();
            RebuildParentIfNeeded();
        }

        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();
            RebuildParentIfNeeded();
        }

        private void RebuildParentIfNeeded()
        {
            if (transform.parent == null) return;

            var parentContentSizeFitter = transform.parent.GetComponent<ContentSizeFitter>();
            if (parentContentSizeFitter != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parentContentSizeFitter.transform as RectTransform);
            }
        }
    }
}