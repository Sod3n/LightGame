using System;
using System.Collections.Generic;
using UnityEngine;

namespace LightGame.Features.VFX
{
    // Squash/stretch and lean on a visual child, kicked by Animator state changes and facing flips.
    // The visual must be a child so the scale never reaches the physics root.
    public class SpriteTransitionSpring : MonoBehaviour
    {
        [Serializable]
        public class Kick
        {
            [Tooltip("Animator state name; empty matches any state.")]
            public string from;
            [Tooltip("Animator state name; empty matches any state.")]
            public string to;
            [Tooltip("Peak stretch: positive is taller and thinner, negative is shorter and wider.")]
            public float stretch;
            [Tooltip("Peak lean in degrees; positive leans forward.")]
            public float lean;
        }

        [SerializeField] private Animator animator;
        [SerializeField] private Transform visual;
        [SerializeField] private int layer;
        [Tooltip("Point in this object's space that stays planted, usually the feet.")]
        [SerializeField] private Vector2 anchor;
        [SerializeField, Min(0.1f)] private float frequency = 4f;
        [SerializeField, Range(0.05f, 0.95f)] private float damping = 0.45f;
        [SerializeField] private float turnStretch = 0.08f;
        [Tooltip("Every matching kick is applied, so a specific rule can add to a generic one.")]
        [SerializeField] private List<Kick> kicks = new();

        private float _stretch, _stretchVelocity, _lean, _leanVelocity;
        private int _lastState;
        private float _lastFacing;
        private float _impulsePerPeak;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            var omega = 2 * Mathf.PI * frequency;
            var dampedOmega = omega * Mathf.Sqrt(1 - damping * damping);
            var peakTime = Mathf.Atan2(dampedOmega, damping * omega) / dampedOmega;
            _impulsePerPeak = dampedOmega / (Mathf.Exp(-damping * omega * peakTime) * Mathf.Sin(dampedOmega * peakTime));
        }

        private void OnDisable()
        {
            _stretch = _stretchVelocity = _lean = _leanVelocity = 0;
            _lastState = 0;
            Apply();
        }

        private void LateUpdate()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;

            var state = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            if (_lastState != 0 && state != _lastState)
            {
                foreach (var k in kicks)
                {
                    if ((string.IsNullOrEmpty(k.from) || Animator.StringToHash(k.from) == _lastState) &&
                        (string.IsNullOrEmpty(k.to) || Animator.StringToHash(k.to) == state))
                    {
                        _stretchVelocity += k.stretch * _impulsePerPeak;
                        _leanVelocity += k.lean * _impulsePerPeak;
                    }
                }
            }
            _lastState = state;

            var facing = Mathf.Sign(transform.lossyScale.x);
            if (_lastFacing != 0 && facing != _lastFacing) _stretchVelocity += turnStretch * _impulsePerPeak;
            _lastFacing = facing;

            Step(ref _stretch, ref _stretchVelocity, Time.deltaTime);
            Step(ref _lean, ref _leanVelocity, Time.deltaTime);
            Apply();
        }

        // Closed-form damped spring step, so the motion doesn't depend on frame rate.
        private void Step(ref float x, ref float v, float dt)
        {
            var omega = 2 * Mathf.PI * frequency;
            var dampedOmega = omega * Mathf.Sqrt(1 - damping * damping);
            var decay = Mathf.Exp(-damping * omega * dt);
            var cos = Mathf.Cos(dampedOmega * dt);
            var sin = Mathf.Sin(dampedOmega * dt);
            var nx = decay * (x * cos + (v + damping * omega * x) / dampedOmega * sin);
            var nv = decay * (v * cos - (omega * omega * x + damping * omega * v) / dampedOmega * sin);
            x = nx;
            v = nv;
        }

        private void Apply()
        {
            if (visual == null) return;
            var height = Mathf.Max(0.2f, 1 + _stretch);
            var scale = new Vector3(1 / Mathf.Sqrt(height), height, 1);
            var rotation = Quaternion.Euler(0, 0, -_lean);
            visual.localScale = scale;
            visual.localRotation = rotation;
            visual.localPosition = (Vector3)anchor - rotation * Vector3.Scale(anchor, scale);
        }
    }
}
