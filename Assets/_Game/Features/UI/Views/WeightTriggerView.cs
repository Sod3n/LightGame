using Core._.UI;
using DG.Tweening;
using LightGame.Features;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LightGame.Features.UI
{
    /// <summary>
    /// Visual feedback for WeightTrigger component.
    /// Changes sprite color and plays scale animation when triggered/untriggered.
    /// </summary>
    public class WeightTriggerView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Light2D light2D;
        [SerializeField] private Light2DTween lightTween;
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

            // Auto-resolve the light references so the grow/residual tween is used even
            // if the serialized links didn't propagate to an existing scene instance.
            if (lightTween == null)
            {
                lightTween = GetComponentInChildren<Light2DTween>(true);
            }
            if (light2D == null)
            {
                light2D = GetComponentInChildren<Light2D>(true);
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
            bool isActive = _weightTrigger != null && _weightTrigger.IsActive;

            if (spriteRenderer != null)
            {
                // Set initial color based on current active state
                spriteRenderer.color = isActive ? activeColor : inactiveColor;
            }

            // Snap the light to its initial state without animating on load.
            SetLightEnabled(isActive, animate: false);
        }
        
        private void OnEnable()
        {
            if (_weightTrigger != null)
            {
                _weightTrigger.OnActivated += OnActivate;
                _weightTrigger.OnDeactivated += OnDeactivate;
            }
        }

        private void OnDisable()
        {
            if (_weightTrigger != null)
            {
                _weightTrigger.OnActivated -= OnActivate;
                _weightTrigger.OnDeactivated -= OnDeactivate;
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

            SetLightEnabled(true);
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

            SetLightEnabled(false);
        }

        /// <summary>
        /// Drives the light so it only shines while active. Prefers the grow/shrink
        /// tween when assigned; otherwise toggles the light instantly.
        /// </summary>
        private void SetLightEnabled(bool enabled, bool animate = true)
        {
            if (lightTween != null)
            {
                if (animate) lightTween.SetActive(enabled);
                else lightTween.SetActiveImmediate(enabled);
                return;
            }

            if (light2D != null)
            {
                light2D.enabled = enabled;
            }
        }
    }
}
