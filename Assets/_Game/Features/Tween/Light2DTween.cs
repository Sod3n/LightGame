using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core._.UI
{
    /// <summary>
    /// Smoothly grows / shrinks a <see cref="Light2D"/> by animating its intensity
    /// (and optionally its outer radius) instead of toggling the light on and off
    /// instantly. Reusable anywhere a light should fade/grow in and out.
    /// </summary>
    public class Light2DTween : TweenableBase
    {
        [SerializeField] private Light2D light2D;
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private Ease growEase = Ease.OutBack;
        [SerializeField] private Ease shrinkEase = Ease.InQuad;

        [Tooltip("Also animate the outer radius, giving a real 'grow' rather than just a fade.")]
        [SerializeField] private bool animateRadius = true;

        [Tooltip("Residual level (0..1) the light keeps while inactive. 0 = fully off, >0 leaves a faint glow.")]
        [Range(0f, 1f)]
        [SerializeField] private float inactiveFactor = 0.15f;

        [Tooltip("Initial state applied on Awake (no animation).")]
        [SerializeField] private bool startActive;

        private float _targetIntensity;
        private float _targetRadius;
        private float _factor; // 0 = fully off/small, 1 = fully on/target

        public override Tween Tween { get; set; }

        private void Awake()
        {
            if (light2D == null)
            {
                light2D = GetComponent<Light2D>();
            }

            if (light2D != null)
            {
                _targetIntensity = light2D.intensity;
                _targetRadius = light2D.pointLightOuterRadius;
            }

            SetActiveImmediate(startActive);
        }

        /// <summary>
        /// Animate the light towards active (grow) or inactive (shrink).
        /// Reversible mid-flight and duration-scaled by how far it has to travel.
        /// </summary>
        public void SetActive(bool active)
        {
            if (light2D == null) return;

            Tween?.Kill();
            Tween = BuildTween(active);
        }

        /// <summary>Snap to the target state with no animation.</summary>
        public void SetActiveImmediate(bool active)
        {
            Tween?.Kill();
            ApplyFactor(active ? 1f : inactiveFactor);
            if (light2D != null) light2D.enabled = active || inactiveFactor > 0f;
        }

        // TweenableBase entry point (e.g. playOnEnable) => grow in.
        public override Tween CreateTween() => BuildTween(true);

        // Revert => shrink out.
        public override void Revert() => SetActive(false);

        private Tween BuildTween(bool active)
        {
            if (light2D == null) return null;

            // Keep the light enabled while animating; only switch it off once fully shrunk.
            light2D.enabled = true;

            float from = _factor;
            float to = active ? 1f : inactiveFactor;
            float scaledDuration = duration * Mathf.Max(Mathf.Abs(to - from), 0.0001f);

            return DOVirtual.Float(from, to, scaledDuration, ApplyFactor)
                .SetEase(active ? growEase : shrinkEase)
                .OnComplete(() =>
                {
                    // Only fully switch the light off when no residual glow is wanted.
                    if (!active && inactiveFactor <= 0f && light2D != null) light2D.enabled = false;
                });
        }

        private void ApplyFactor(float f)
        {
            _factor = f;
            if (light2D == null) return;

            light2D.intensity = _targetIntensity * f;
            if (animateRadius)
            {
                light2D.pointLightOuterRadius = _targetRadius * f;
            }
        }
    }
}
