using UnityEngine;
using UnityEngine.Events;

namespace LightGame.Audio
{
    /// <summary>
    /// ScriptableObject that stores sound data and provides methods to play/stop sounds.
    /// Acts as a mediator between the game and the underlying audio system (Unity/FMOD).
    /// </summary>
    [CreateAssetMenu(fileName = "NewSound", menuName = "LightGame/Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        [Header("Sound Settings")]
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private bool loop = false;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(-3f, 3f)] private float pitch = 1f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;
        [SerializeField, Range(0f, 500f)] private float maxDistance = 50f;

        [Header("Events")]
        public UnityEvent onSoundStart = new UnityEvent();
        public UnityEvent onSoundEnd = new UnityEvent();

        // Reference to the active sound player
        private ISoundPlayer activeSoundPlayer;

        #region Properties
        public AudioClip AudioClip => audioClip;
        public bool Loop => loop;
        public float Volume => volume;

        public float Pitch
        {
            get => pitch;
            set => pitch = value;
        }
        public float SpatialBlend => spatialBlend;
        public float MaxDistance => maxDistance;
        #endregion

        /// <summary>
        /// Plays the sound using the SoundManager.
        /// </summary>
        public void Play()
        {
            SoundManager.PlaySound(this);
        }

        /// <summary>
        /// Plays the sound at a specific position in 3D space.
        /// </summary>
        /// <param name="position">World position for 3D sound</param>
        public void PlayAtPosition(Vector3 position)
        {
            SoundManager.PlaySoundAtPosition(this, position);
        }

        /// <summary>
        /// Stops the currently playing sound.
        /// </summary>
        public void Stop()
        {
            if (activeSoundPlayer != null)
            {
                activeSoundPlayer.Stop();
                activeSoundPlayer = null;
            }
        }

        /// <summary>
        /// Checks if this sound is currently playing.
        /// </summary>
        public bool IsPlaying()
        {
            return activeSoundPlayer != null && activeSoundPlayer.IsPlaying();
        }

        #region Internal Methods (Called by SoundManager)
        /// <summary>
        /// Internal method called by SoundManager to play the sound with a specific player.
        /// </summary>
        internal void PlayInternal(ISoundPlayer player, Vector3? position = null)
        {
            if (audioClip == null)
            {
                Debug.LogWarning($"Cannot play sound '{name}': AudioClip is null", this);
                return;
            }

            Stop(); // Stop any currently playing instance

            activeSoundPlayer = player;
            activeSoundPlayer.Play(this, position);

            onSoundStart?.Invoke();
        }

        /// <summary>
        /// Called by the sound player when the sound finishes playing.
        /// </summary>
        internal void OnSoundFinished()
        {
            activeSoundPlayer = null;
            onSoundEnd?.Invoke();
        }
        #endregion
    }
}
