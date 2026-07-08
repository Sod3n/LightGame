using Core._.UI;
using DG.Tweening;
using Light_and_controller.Scripts.Components;
using UnityEngine;

namespace Light_and_controller.Scripts.UI
{
    /// <summary>
    /// Visual feedback for WeightTrigger component.
    /// Changes sprite color and plays scale animation when triggered/untriggered.
    /// </summary>
    public class WeightTriggerView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TweenableBase activateScaleTween;
        [SerializeField] private TweenableBase deactivateScaleTween;
        
        [Header("Color Settings")]
        [SerializeField] private Color inactiveColor = Color.gray;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private float colorTransitionDuration = 0.3f;
        [SerializeField] private Ease colorEase = Ease.OutQuad;
        
        private Tween _colorTween;
        private WeightTrigger _weightTrigger;
        
        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            // Auto-connect to WeightTrigger component
            _weightTrigger = GetComponent<WeightTrigger>();
            if (_weightTrigger == null)
            {
                Debug.LogWarning("WeightTriggerView: No WeightTrigger component found on this GameObject!", this);
            }
        }
        
        private void Start()
        {
            // Initialize color based on WeightTrigger's initial state
            // The WeightTrigger.Start() will invoke onActivate/onDeactivate events
            // which will set the correct color through the event listeners
            if (_weightTrigger != null && spriteRenderer != null)
            {
                // Set initial color based on current active state
                spriteRenderer.color = _weightTrigger.IsActive ? activeColor : inactiveColor;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = inactiveColor;
            }
        }
        
        private void OnEnable()
        {
            if (_weightTrigger != null)
            {
                _weightTrigger.onActivate.AddListener(OnActivate);
                _weightTrigger.onDeactivate.AddListener(OnDeactivate);
            }
        }
        
        private void OnDisable()
        {
            if (_weightTrigger != null)
            {
                _weightTrigger.onActivate.RemoveListener(OnActivate);
                _weightTrigger.onDeactivate.RemoveListener(OnDeactivate);
            }
        }
        
        private void OnDestroy()
        {
            _colorTween?.Kill();
        }
        
        /// <summary>
        /// Called when the weight trigger is activated.
        /// </summary>
        public void OnActivate()
        {
            // Kill any existing color tween
            _colorTween?.Kill();
            
            // Animate color to active
            if (spriteRenderer != null)
            {
                _colorTween = spriteRenderer.DOColor(activeColor, colorTransitionDuration)
                    .SetEase(colorEase);
            }
            
            // Play activate scale animation
            if (activateScaleTween != null)
            {
                activateScaleTween.Play();
            }
        }
        
        /// <summary>
        /// Called when the weight trigger is deactivated.
        /// </summary>
        public void OnDeactivate()
        {
            // Kill any existing color tween
            _colorTween?.Kill();
            
            // Animate color to inactive
            if (spriteRenderer != null)
            {
                _colorTween = spriteRenderer.DOColor(inactiveColor, colorTransitionDuration)
                    .SetEase(colorEase);
            }
            
            // Play deactivate scale animation
            if (deactivateScaleTween != null)
            {
                deactivateScaleTween.Play();
            }
        }
    }
}
