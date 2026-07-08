using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Tween for jump pad bounce animation when player bounces off.
    /// Creates a stretch and return effect by scaling the jump pad.
    /// </summary>
    public class JumpPadBounceTween : TweenableBase
    {
        [SerializeField] private Transform tweenTarget;
        [SerializeField] private Vector3 bounceScale = new Vector3(0.8f, 1.3f, 1f);
        [SerializeField] private Vector3 targetScale = Vector3.one;
        [SerializeField] private float bounceDuration = 0.3f;
        [SerializeField] private float returnDuration = 0.15f;
        [SerializeField] private Ease bounceEase = Ease.OutElastic;
        [SerializeField] private Ease returnEase = Ease.OutQuad;
        
        [Header("Position Movement")]
        [SerializeField] private bool enablePositionBounce = true;
        [SerializeField] private float moveUpDistance = 0.2f;
        [SerializeField] private float moveUpDuration = 0.1f;
        [SerializeField] private Ease moveUpEase = Ease.OutQuad;
        
        [Header("Additional Movement")]
        [SerializeField] private bool enablePunchScale = true;
        [SerializeField] private float punchStrength = 0.2f;
        [SerializeField] private int punchVibrato = 3;
        [SerializeField] private float punchElasticity = 0.5f;
        
        public override Tween Tween { get; set; }

        private void Awake()
        {
            if (tweenTarget == null)
                tweenTarget = transform;
        }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            Vector3 currentPosition = tweenTarget.localPosition;
            
            // Move up to simulate pushing the player
            if (enablePositionBounce)
            {
                sequence.Append(tweenTarget.DOLocalMoveY(currentPosition.y + moveUpDistance, moveUpDuration).SetEase(moveUpEase));
                sequence.Append(tweenTarget.DOLocalMoveY(currentPosition.y, returnDuration).SetEase(returnEase));
            }
            
            // Bounce to stretched scale from current scale (plays with position movement)
            sequence.Join(tweenTarget.DOScale(bounceScale, bounceDuration).SetEase(bounceEase));
            // Return to target scale
            sequence.Append(tweenTarget.DOScale(targetScale, returnDuration).SetEase(returnEase));
            
            // Add punch scale for extra bounce feel
            if (enablePunchScale)
            {
                sequence.Join(tweenTarget.DOPunchScale(Vector3.one * punchStrength, returnDuration, punchVibrato, punchElasticity));
            }
            
            sequence.SetTarget(tweenTarget);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            
            return sequence;
        }
    }
}
