using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Tween for jump pad ready animation when cooldown completes.
    /// Creates a subtle pulse effect to indicate the pad is ready to use again.
    /// </summary>
    public class JumpPadReadyTween : TweenableBase
    {
        [SerializeField] private Transform tweenTarget;
        [SerializeField] private Vector3 pulseScale = new Vector3(1.1f, 1.1f, 1f);
        [SerializeField] private Vector3 targetScale = Vector3.one;
        [SerializeField] private float pulseDuration = 0.4f;
        [SerializeField] private Ease pulseEase = Ease.InOutSine;

        public override Tween Tween { get; set; }

        private void Awake()
        {
            if (tweenTarget == null)
                tweenTarget = transform;
        }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            
            // Pulse up from current scale
            sequence.Append(tweenTarget.DOScale(pulseScale, pulseDuration).SetEase(pulseEase));
            // Pulse down to target scale
            sequence.Append(tweenTarget.DOScale(targetScale, pulseDuration).SetEase(pulseEase));
            
            sequence.SetTarget(tweenTarget);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            
            return sequence;
        }
    }
}
