
using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Tween for animating the width and height of a UI RectTransform element.
    /// Changes the sizeDelta property to resize the UI element.
    /// </summary>
    public class SizeDeltaTween : TweenableBase
    {
        [SerializeField] private bool _useStartSize;
        [SerializeField] private Vector2 _startSize;
        [SerializeField] private Vector2 _endSize;
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private Ease _sizeEase = Ease.OutQuad;

        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            var rect = (RectTransform)transform.parent;
            
            if (_useStartSize)
            {
                // Set start size with 0-duration tween, then animate to end size
                sequence.Append(rect.DOSizeDelta(_startSize, 0.001f));
            }
            
            sequence.Append(rect.DOSizeDelta(_endSize, _duration).SetEase(_sizeEase));
            
            return sequence;
        }
    }
}
