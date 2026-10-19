using FMODUnity;
using UnityEngine;

namespace LightGame.Features.Audio
{
    // Plays an FMOD one-shot each time the Animator enters one of the given states, e.g. the
    // attack swing for both the ground and the air attack.
    public class AnimatorStateSound : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private int layer;
        [Tooltip("Animator state names that trigger the sound.")]
        [SerializeField] private string[] states;
        [SerializeField] private EventReference sound;

        private int[] _hashes;
        private int _lastState;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            _hashes = new int[states.Length];
            for (var i = 0; i < states.Length; i++) _hashes[i] = Animator.StringToHash(states[i]);
        }

        private void OnDisable() => _lastState = 0;

        private void LateUpdate()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;

            var current = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            if (current != _lastState && System.Array.IndexOf(_hashes, current) >= 0 && !sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
            _lastState = current;
        }
    }
}
