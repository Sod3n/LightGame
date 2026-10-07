using System;
using System.Collections.Generic;
using UnityEngine;

namespace LightGame.Features.VFX
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimationCrossfade : MonoBehaviour
    {
        [Serializable]
        public class Transition
        {
            [Tooltip("Animator state name; empty matches any state.")]
            public string from;
            [Tooltip("Animator state name; empty matches any state.")]
            public string to;
            [Min(0)] public float duration;
        }

        [SerializeField] private Animator animator;
        [SerializeField] private int layer;
        [SerializeField, Min(0)] private float defaultDuration = 0.1f;
        [Tooltip("Opacity of the outgoing frame at the moment of the switch. Below 1 the new pose shows through immediately.")]
        [SerializeField, Range(0, 1)] private float startOpacity = 0.6f;
        [Tooltip("First match wins. Set duration to 0 to keep a hard cut.")]
        [SerializeField] private List<Transition> transitions = new();

        private SpriteRenderer _renderer;
        private SpriteRenderer _ghost;
        private int _lastState;
        private Sprite _lastSprite;
        private float _fadeElapsed;
        private float _fadeDuration;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponentInParent<Animator>();

            var go = new GameObject("CrossfadeGhost");
            go.transform.SetParent(transform, false);
            _ghost = go.AddComponent<SpriteRenderer>();
            _ghost.enabled = false;
        }

        private void OnDisable()
        {
            if (_ghost != null) _ghost.enabled = false;
            _lastState = 0;
        }

        private void LateUpdate()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;

            var state = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            if (_lastState != 0 && state != _lastState && _lastSprite != null && _lastSprite != _renderer.sprite)
            {
                var duration = DurationFor(_lastState, state);
                if (duration > 0) StartFade(_lastSprite, duration);
            }
            _lastState = state;

            if (_ghost.enabled)
            {
                var t = _fadeElapsed / _fadeDuration;
                if (t >= 1)
                {
                    _ghost.enabled = false;
                }
                else
                {
                    var c = _renderer.color;
                    c.a *= startOpacity * (1 - t);
                    _ghost.color = c;
                }
                _fadeElapsed += Time.deltaTime;
            }

            _lastSprite = _renderer.sprite;
        }

        private float DurationFor(int from, int to)
        {
            foreach (var tr in transitions)
            {
                if ((string.IsNullOrEmpty(tr.from) || Animator.StringToHash(tr.from) == from) &&
                    (string.IsNullOrEmpty(tr.to) || Animator.StringToHash(tr.to) == to))
                    return tr.duration;
            }
            return defaultDuration;
        }

        private void StartFade(Sprite sprite, float duration)
        {
            _ghost.sprite = sprite;
            _ghost.sharedMaterial = _renderer.sharedMaterial;
            _ghost.sortingLayerID = _renderer.sortingLayerID;
            _ghost.sortingOrder = _renderer.sortingOrder + 1;
            _ghost.flipX = _renderer.flipX;
            _ghost.flipY = _renderer.flipY;
            _ghost.maskInteraction = _renderer.maskInteraction;
            _ghost.enabled = true;
            _fadeElapsed = 0;
            _fadeDuration = duration;
        }
    }
}
