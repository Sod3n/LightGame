using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    public class PulseTween : TweenableBase
    {
        [SerializeField] private float pulsePower = 0.1f;
        [SerializeField] private Ease pulseEase = Ease.InSine;
        [SerializeField] private Vector3 startScale = Vector3.one;
        
        public override Tween Tween { get; set; }
    
        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            if (Vector3.Distance(transform.localScale, startScale) > 0.001f)
            {
                sequence.Append(transform.DOScale(startScale, 0.5f)
                    .SetEase(pulseEase));
                sequence.AppendCallback(() =>
                {
                    CreatePulseLoop();
                });
                sequence.SetTarget(transform);
                sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
                return sequence;
            }
    
            return CreatePulseLoop();
        }
        
        private Tween CreatePulseLoop()
        {
            var sequence = DOTween.Sequence();
            sequence
                .Append(transform.DOPunchScale(Vector3.one * pulsePower, 1f, 1))
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .SetEase(pulseEase)
                .SetTarget(transform)
                .SetLoops(-1, LoopType.Restart);
            return sequence;
        }
    }
}