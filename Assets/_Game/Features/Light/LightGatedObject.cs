using System.Collections.Generic;
using Core._.UI;
using DG.Tweening;
using LightGame.Core;
using LightGame.Globals;
using UnityEngine;

namespace LightGame.Features
{
    /// <summary>
    /// Reveals (or hides) this object based on whether a light source is currently reaching its
    /// <see cref="LightDetector"/>. Composition of existing mechanics: LightDetector senses the
    /// light (occlusion-aware) and raises <see cref="LightChangeEvent"/>; this listens and fades
    /// the object's body in/out so, e.g., an Activator only "appears" while another light shines on it.
    ///
    /// The fade reuses existing tweens: sprite alpha is cross-faded and the light rides the project's
    /// <see cref="Light2DTween"/>. The LightDetector and this GameObject's trigger Collider2D are
    /// intentionally NOT toggled — they must stay enabled so the object keeps sensing light while
    /// hidden. Only the visuals and <see cref="gatedBehaviours"/> (interactivity) are switched.
    ///
    /// Runs late (see .meta executionOrder) so it re-asserts the hidden state after sibling views
    /// such as WeightTriggerView set the sprite colour in their own Start.
    /// </summary>
    [RequireComponent(typeof(LightDetector))]
    public class LightGatedObject : MonoBehaviour
    {
        [Header("Gating")]
        [SerializeField, Tooltip("ON: revealed while in light (appears only when lit). OFF: inverted — hidden while in light.")]
        private bool revealInLight = true;
        [SerializeField, Tooltip("ON: react only to normal (Default) lights. OFF: any light, including LevelChange, counts.")]
        private bool defaultLightOnly = false;

        [Header("Fade")]
        [SerializeField, Tooltip("Seconds for the appear/disappear fade.")]
        private float fadeDuration = 0.35f;
        [SerializeField, Tooltip("Ease applied to the sprite alpha fade.")]
        private Ease fadeEase = Ease.OutQuad;

        [Header("Targets")]
        [SerializeField, Tooltip("Sprites cross-faded with the light (the visible body, e.g. Skin).")]
        private List<SpriteRenderer> fadeSprites = new List<SpriteRenderer>();
        [SerializeField, Tooltip("Optional Light2DTween faded in/out with the body (grow / shrink the Light2D).")]
        private Light2DTween lightTween;
        [SerializeField, Tooltip("Behaviours enabled/disabled with the light (e.g. WeightTrigger) — interactivity gating.")]
        private List<Behaviour> gatedBehaviours = new List<Behaviour>();

        private readonly List<float> _baseAlpha = new List<float>();
        private Sequence _fade;
        private bool _revealed;

        public bool IsRevealed => _revealed;

        private void Awake()
        {
            foreach (var sr in fadeSprites)
                _baseAlpha.Add(sr != null ? sr.color.a : 1f);

            // Start hidden so the object only appears once light actually arrives.
            ApplyImmediate(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
            _fade?.Kill();
        }

        private void Start()
        {
            // Re-assert hidden state after other components' Start (WeightTriggerView sets the
            // sprite colour on Start, which would otherwise clear our faded-out alpha at load).
            if (!_revealed)
                ApplyImmediate(false);
        }

        private void OnLightChangeEvent(LightChangeEvent evt)
        {
            if (defaultLightOnly)
            {
                // React to the Default per-type event only.
                if (evt.LightType != LightType.Default) return;
            }
            else
            {
                // LightDetector also publishes per-type events; react to the aggregate
                // "any light" event (LightType == null) so we don't fade twice.
                if (evt.LightType != null) return;
            }

            SetRevealed(revealInLight ? evt.IsInLight : !evt.IsInLight);
        }

        private void SetRevealed(bool revealed)
        {
            if (_revealed == revealed) return;
            _revealed = revealed;

            SetBehaviours(revealed);

            _fade?.Kill();
            _fade = DOTween.Sequence();
            for (int i = 0; i < fadeSprites.Count; i++)
            {
                var sr = fadeSprites[i];
                if (sr == null) continue;
                float target = revealed ? _baseAlpha[i] : 0f;
                _fade.Join(sr.DOFade(target, fadeDuration).SetEase(fadeEase));
            }

            if (lightTween != null)
                lightTween.SetActive(revealed);
        }

        private void ApplyImmediate(bool revealed)
        {
            _revealed = revealed;
            _fade?.Kill();

            for (int i = 0; i < fadeSprites.Count; i++)
            {
                var sr = fadeSprites[i];
                if (sr == null) continue;
                var c = sr.color;
                c.a = revealed ? _baseAlpha[i] : 0f;
                sr.color = c;
            }

            if (lightTween != null)
                lightTween.SetActiveImmediate(revealed);

            SetBehaviours(revealed);
        }

        private void SetBehaviours(bool on)
        {
            foreach (var b in gatedBehaviours)
                if (b != null) b.enabled = on;
        }
    }
}
