using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core._.UI
{
    /// <summary>
    /// Tween for health segment UI animation.
    /// Moves anchored position upward, scales the element, and changes colors of two images.
    /// </summary>
    public class HealthSegmentTween : TweenableBase
    {
        [Header("Target Components")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image firstImage;
        [SerializeField] private Image secondImage;
        
        [Header("Position Animation")]
        [SerializeField] private float moveYOffset = 7f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease moveEase = Ease.OutBack;
        [SerializeField] private bool returnToOriginal = true;
        [SerializeField] private float returnDelay = 0.2f;
        [SerializeField] private float returnDuration = 0.25f;
        [SerializeField] private Ease returnEase = Ease.InOutQuad;
        
        [Header("Scale Animation")]
        [SerializeField] private bool useStartScale;
        [SerializeField] private Vector3 startScale = Vector3.one;
        [SerializeField] private Vector3 targetScale = new Vector3(1.15f, 1.15f, 1f);
        [SerializeField] private float scaleDuration = 0.3f;
        [SerializeField] private Ease scaleEase = Ease.OutBack;
        
        [Header("Color Animation")]
        [SerializeField] private Color firstImageTargetColor = Color.green;
        [SerializeField] private Color secondImageTargetColor = Color.yellow;
        [SerializeField] private float colorDuration = 0.3f;
        [SerializeField] private Ease colorEase = Ease.OutQuad;
        
        // Store original values for rewind capability
        private Vector2 _originalAnchoredPos;
        private Vector3 _originalScale;
        private Color _originalFirstImageColor;
        private Color _originalSecondImageColor;
        private bool _isInitialized = false;
        
        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            if (rectTransform == null)
                rectTransform = transform.parent.GetComponent<RectTransform>();
            
            if (rectTransform == null)
            {
                Debug.LogWarning("HealthSegmentTween: RectTransform is not assigned!", this);
                return null;
            }
            
            // Initialize original values only once (on first tween creation)
            if (!_isInitialized)
            {
                _originalAnchoredPos = rectTransform.anchoredPosition;
                _originalScale = rectTransform.localScale;
                _originalFirstImageColor = firstImage != null ? firstImage.color : Color.white;
                _originalSecondImageColor = secondImage != null ? secondImage.color : Color.white;
                _isInitialized = true;
            }
            
            // Set start scale if enabled
            if (useStartScale)
            {
                rectTransform.localScale = startScale;
            }
            
            var sequence = DOTween.Sequence();
            
            // Move anchored position up by Y offset
            Vector2 targetPosition = new Vector2(_originalAnchoredPos.x, _originalAnchoredPos.y + moveYOffset);
            sequence.Append(rectTransform.DOAnchorPos(targetPosition, moveDuration).SetEase(moveEase));
            
            // Scale animation (plays simultaneously with position)
            var scaleFrom = useStartScale ? startScale : rectTransform.localScale;
            sequence.Join(rectTransform.DOScale(targetScale, scaleDuration).SetEase(scaleEase));
            
            // Color animations for both images (plays simultaneously)
            if (firstImage != null)
            {
                sequence.Join(firstImage.DOColor(firstImageTargetColor, colorDuration).SetEase(colorEase));
            }
            
            if (secondImage != null)
            {
                sequence.Join(secondImage.DOColor(secondImageTargetColor, colorDuration).SetEase(colorEase));
            }
            
            // Return to original state if enabled
            if (returnToOriginal)
            {
                sequence.AppendInterval(returnDelay);
                
                // Return position
                sequence.Append(rectTransform.DOAnchorPos(_originalAnchoredPos, returnDuration).SetEase(returnEase));
                
                // Return scale
                sequence.Join(rectTransform.DOScale(_originalScale, returnDuration).SetEase(returnEase));
                
                // Return colors
                if (firstImage != null)
                {
                    sequence.Join(firstImage.DOColor(_originalFirstImageColor, returnDuration).SetEase(returnEase));
                }
                
                if (secondImage != null)
                {
                    sequence.Join(secondImage.DOColor(_originalSecondImageColor, returnDuration).SetEase(returnEase));
                }
            }
            
            sequence.SetTarget(rectTransform);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            
            return sequence;
        }
        
        public override void Revert()
        {
            if (!_isInitialized || rectTransform == null)
                return;
            
            // Kill any active tweens on this transform
            rectTransform.DOKill();
            
            // Immediately restore to original values
            rectTransform.anchoredPosition = _originalAnchoredPos;
            rectTransform.localScale = _originalScale;
            
            if (firstImage != null)
            {
                firstImage.color = _originalFirstImageColor;
            }
            
            if (secondImage != null)
            {
                secondImage.color = _originalSecondImageColor;
            }
        }
    }
}
