using System;
using System.Collections.Generic;
using UnityEngine;

namespace LightGame.Features.VFX
{
    // Burns the sprites away with the light-dissolve edge, holding the pose they had when death hit:
    // outward from the hit point when there is one, otherwise from the outside in. Reset puts the
    // original materials back for the respawn.
    public class DeathDissolve : MonoBehaviour
    {
        private static readonly int CutoutSpheres = Shader.PropertyToID("_CutoutSpheres");
        private static readonly int EdgeWidth = Shader.PropertyToID("_EdgeWidth");
        private static readonly int EdgeNoiseStrength = Shader.PropertyToID("_EdgeNoiseStrength");
        private static readonly int DissolveInvert = Shader.PropertyToID("_DissolveInvert");

        [SerializeField] private Animator animator;
        [Tooltip("Every SpriteRenderer under this root dissolves.")]
        [SerializeField] private Transform visual;
        [Tooltip("Material on the LightGame/2D/SpriteLitDissolve shader.")]
        [SerializeField] private Material dissolveMaterial;
        [SerializeField, Min(0.05f)] private float duration = 0.9f;
        [SerializeField] private AnimationCurve progress = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private const float BodyPadding = 0.3f;

        private readonly List<(SpriteRenderer renderer, Material material)> _swapped = new();
        private readonly Vector4[] _spheres = new Vector4[4];
        private Material _instance;
        private Action _onComplete;
        private Vector2 _center;
        private float _startRadius, _endRadius, _elapsed;
        private bool _playing;
        private bool _frozeAnimator;

        public void Play(Action onComplete, Vector2? origin = null)
        {
            if (_playing || dissolveMaterial == null || visual == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (animator == null) animator = GetComponent<Animator>();
            // Disabled rather than paused: a paused Animator still takes the switch to the Death state.
            _frozeAnimator = animator != null && animator.enabled;
            if (_frozeAnimator) animator.enabled = false;

            if (_instance == null) _instance = new Material(dissolveMaterial);
            foreach (var sprite in visual.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!sprite.enabled || sprite.sprite == null) continue;
                _swapped.Add((sprite, sprite.sharedMaterial));
                sprite.sharedMaterial = _instance;
            }

            // Measure the body (the collider, padded for the hood and staff), not the sprite cells,
            // which carry wide empty margins.
            var body = TryGetComponent<Collider2D>(out var collider) ? collider.bounds : new Bounds(visual.position, Vector3.one);
            body.Expand(BodyPadding * 2f);

            // The rim starts and ends just off the body, so the first and last frames show no edge.
            var margin = _instance.GetFloat(EdgeWidth) + _instance.GetFloat(EdgeNoiseStrength);
            if (origin is Vector2 hit)
            {
                // A hole opens at the hit and grows until it has eaten the farthest corner.
                _instance.SetFloat(DissolveInvert, 1f);
                _center = hit;
                _startRadius = -margin;
                _endRadius = FarthestCorner(body, hit) + margin;
            }
            else
            {
                // No hit point (darkness): shrink a sphere around the body so it burns inward.
                _instance.SetFloat(DissolveInvert, 0f);
                _center = body.center;
                _startRadius = ((Vector2)body.extents).magnitude + margin;
                _endRadius = -margin;
            }
            _elapsed = 0f;
            _onComplete = onComplete;
            _playing = true;
            Apply(0f);
        }

        public void ResetVisual()
        {
            _playing = false;
            _onComplete = null;
            foreach (var (sprite, material) in _swapped)
                if (sprite != null) sprite.sharedMaterial = material;
            _swapped.Clear();
            if (_frozeAnimator && animator != null) animator.enabled = true;
            _frozeAnimator = false;
        }

        private void Update()
        {
            if (!_playing) return;
            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / duration);
            Apply(progress.Evaluate(t));
            if (t < 1f) return;

            _playing = false;
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        private void Apply(float amount)
        {
            _spheres[0] = new Vector4(_center.x, _center.y, 0f, Mathf.Lerp(_startRadius, _endRadius, amount));
            for (var i = 1; i < _spheres.Length; i++) _spheres[i] = new Vector4(1e5f, 1e5f, 0f, 0f);
            _instance.SetVectorArray(CutoutSpheres, _spheres);
        }

        private static float FarthestCorner(Bounds b, Vector2 from)
        {
            var dx = Mathf.Max(Mathf.Abs(b.min.x - from.x), Mathf.Abs(b.max.x - from.x));
            var dy = Mathf.Max(Mathf.Abs(b.min.y - from.y), Mathf.Abs(b.max.y - from.y));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private void OnDisable() => ResetVisual();

        private void OnDestroy()
        {
            if (_instance != null) Destroy(_instance);
        }
    }
}
