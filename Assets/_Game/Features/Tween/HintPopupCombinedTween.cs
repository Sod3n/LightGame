using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Tween for showing a hint popup from the bottom of the screen using both slide and scale animations.
    /// Combines sliding up from below with a scale effect for a more dynamic appearance.
    /// </summary>
    public class HintPopupCombinedTween : TweenableBase
    {
        [SerializeField] private Vector2 _startPos;
        [SerializeField] private Vector2 _endPos;
        [SerializeField] private Vector3 _startScale = new Vector3(0.8f, 0.8f, 0.8f);
        [SerializeField] private Vector3 _targetScale = Vector3.one;
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _scaleEase = Ease.OutElastic;

        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            var rect = (RectTransform)transform.parent;

            // Set initial state using tweens (instant)
            sequence.Append(rect.DOScale(_startScale, 0f));
            sequence.Join(rect.DOAnchorPos(_startPos, 0f));

            // Add slide animation
            sequence.Append(rect.DOAnchorPos(_endPos, _duration).SetEase(_showEase));

            // Add scale animation (runs simultaneously with slide)
            sequence.Join(rect.DOScale(_targetScale, _duration).SetEase(_scaleEase));

            return sequence;
        }

        /// <summary>
        /// Hides the popup by sliding it down and scaling it down
        /// </summary>
        public void Hide(float hideDuration = 0.4f)
        {
            Tween?.Complete();

            var sequence = DOTween.Sequence();
            var rect = (RectTransform)transform;

            sequence.Append(rect.DOAnchorPos(_startPos, hideDuration).SetEase(Ease.InBack));
            sequence.Join(transform.DOScale(_startScale, hideDuration).SetEase(Ease.InBack));

            Tween = sequence;
        }
    }
}
