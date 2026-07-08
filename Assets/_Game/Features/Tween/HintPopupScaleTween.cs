using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Tween for showing a hint popup from the bottom of the screen using scale animation.
    /// The popup scales from 0 to 1 with a bounce effect.
    /// </summary>
    public class HintPopupScaleTween : TweenableBase
    {
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Vector3 startScale = Vector3.zero;
        [SerializeField] private Vector3 targetScale = Vector3.one;

        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();

            // Set initial scale using tween (instant)
            sequence.Append(transform.DOScale(startScale, 0f));
            sequence.Append(transform.DOScale(targetScale, duration)
                .SetEase(showEase));

            sequence.SetTarget(transform);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            return sequence;
        }

        /// <summary>
        /// Hides the popup by scaling it back down
        /// </summary>
        public void Hide(float hideDuration = 0.3f)
        {
            Tween?.Complete();
            Tween = transform.DOScale(startScale, hideDuration)
                .SetEase(Ease.InBack)
                .SetTarget(transform)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
    }
}
