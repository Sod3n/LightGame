using UnityEngine;

namespace LightGame.Features.VFX
{
    // Plays a sprite sequence where an Animator state started and leaves it there, e.g. the dash
    // trail the player runs away from. Runs on its own clock, so it finishes after the state ends.
    public class WorldAnchoredStateEffect : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private int layer;
        [Tooltip("Animator state that starts the effect.")]
        [SerializeField] private string state;
        [Tooltip("Child renderer that plays the effect; it is detached from the player while playing.")]
        [SerializeField] private SpriteRenderer effect;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1)] private float framesPerSecond = 24f;

        private Transform _home;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Vector3 _homeScale;
        private int _stateHash;
        private int _lastState;
        private float _elapsed;
        private bool _playing;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            _stateHash = Animator.StringToHash(state);
            _home = effect.transform.parent;
            _homePosition = effect.transform.localPosition;
            _homeRotation = effect.transform.localRotation;
            _homeScale = effect.transform.localScale;
            effect.sprite = null;
        }

        private void OnDisable()
        {
            Stop();
            _lastState = 0;
        }

        private void OnDestroy()
        {
            // While playing the renderer lives at the scene root, so it would outlive the player.
            if (effect != null && effect.transform.parent != _home) Destroy(effect.gameObject);
        }

        private void LateUpdate()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;

            var current = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            if (current == _stateHash && _lastState != _stateHash) Play();
            _lastState = current;

            if (!_playing) return;
            _elapsed += Time.deltaTime;
            var frame = (int)(_elapsed * framesPerSecond);
            if (frame >= frames.Length) Stop();
            else effect.sprite = frames[frame];
        }

        private void Play()
        {
            Stop();
            var t = effect.transform;
            var scale = t.lossyScale;
            t.SetParent(null, true);
            t.localScale = scale;
            _elapsed = 0;
            _playing = frames.Length > 0;
            if (_playing) effect.sprite = frames[0];
        }

        private void Stop()
        {
            _playing = false;
            if (effect == null) return;
            effect.sprite = null;
            var t = effect.transform;
            if (t.parent == _home || _home == null) return;
            t.SetParent(_home, false);
            t.localPosition = _homePosition;
            t.localRotation = _homeRotation;
            t.localScale = _homeScale;
        }
    }
}
