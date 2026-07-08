using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Tween for showing a hint popup from the bottom of the screen using slide animation.
    /// The popup slides up from below the screen with an optional bounce effect.
    /// </summary>
    public class HintPopupSlideTween : TweenableBase
    {
        [SerializeField] private Vector2 _startPos;
        [SerializeField] private Vector2 _endPos;
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private Ease _showEase = Ease.OutBack;

        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            var rect = (RectTransform)transform;

            sequence.Append(rect.DOAnchorPos(_startPos, 0f));
            sequence.Append(rect.DOAnchorPos(_endPos, _duration).SetEase(_showEase));
            
            return sequence;
        }
    }
}
