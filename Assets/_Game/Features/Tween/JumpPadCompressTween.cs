using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Tween for jump pad compress animation when player lands.
    /// Creates a squash effect by scaling the jump pad.
    /// </summary>
    public class JumpPadCompressTween : TweenableBase
    {
        [SerializeField] private Transform tweenTarget;
        [SerializeField] private Vector3 compressScale = new Vector3(1.2f, 0.7f, 1f);
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        public override Tween Tween { get; set; }

        private void Awake()
        {
            if (tweenTarget == null)
                tweenTarget = transform;
        }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            
            // Compress from current scale
            sequence.Append(tweenTarget.DOScale(compressScale, duration).SetEase(ease));
            
            sequence.SetTarget(tweenTarget);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            
            return sequence;
        }
    }
}
