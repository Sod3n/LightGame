using UnityEngine;

namespace LightGame.Features.VFX
{
    // Slow breathing of a glow made of sprites, e.g. the red tint and halo that keep spikes readable
    // in the dark. Sprites rather than a Light2D, because a 2D light also tints whatever is drawn in
    // front of the spike. Each instance starts at a random phase so neighbouring spikes don't pulse
    // in sync.
    public class GlowPulse : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] sprites;
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of the base alpha the pulse swings above and below it.")]
        private float amount = 0.25f;
        [SerializeField, Min(0.01f), Tooltip("Pulses per second.")]
        private float frequency = 0.5f;

        private float[] _baseAlpha;
        private float _phase;

        private void Awake()
        {
            _baseAlpha = new float[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) _baseAlpha[i] = sprites[i].color.a;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void OnDisable() => Apply(1f);

        private void Update() => Apply(1f + amount * Mathf.Sin(_phase + Time.time * frequency * Mathf.PI * 2f));

        private void Apply(float factor)
        {
            if (_baseAlpha == null) return;
            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                var c = sprites[i].color;
                c.a = Mathf.Clamp01(_baseAlpha[i] * factor);
                sprites[i].color = c;
            }
        }
    }
}
