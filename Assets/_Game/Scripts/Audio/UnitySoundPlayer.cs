using System.Collections;
using UnityEngine;

namespace LightGame.Audio
{
    /// <summary>
    /// Unity AudioSource-based implementation of ISoundPlayer.
    /// This will be replaced with FMODSoundPlayer when migrating to FMOD.
    /// </summary>
    public class UnitySoundPlayer : MonoBehaviour, ISoundPlayer
    {
        private AudioSource audioSource;
        private SoundData currentSoundData;
        private Coroutine soundEndCheckCoroutine;

        private void Awake()
        {
            // Get or create AudioSource component
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
        }

        public void Play(SoundData soundData, Vector3? position = null)
        {
            if (soundData == null || soundData.AudioClip == null)
            {
                Debug.LogWarning("Cannot play sound: SoundData or AudioClip is null");
                return;
            }

            Stop(); // Stop any currently playing sound

            currentSoundData = soundData;

            // Configure AudioSource
            audioSource.clip = soundData.AudioClip;
            audioSource.loop = soundData.Loop;
            audioSource.volume = soundData.Volume;
            audioSource.pitch = soundData.Pitch;
            audioSource.spatialBlend = soundData.SpatialBlend;
            audioSource.maxDistance = soundData.MaxDistance;

            // Set position if provided
            if (position.HasValue)
            {
                transform.position = position.Value;
            }

            audioSource.Play();

            // Start coroutine to check when sound ends (if not looping)
            if (!soundData.Loop)
            {
                soundEndCheckCoroutine = StartCoroutine(CheckSoundEnd());
            }
        }

        public void Stop()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (soundEndCheckCoroutine != null)
            {
                StopCoroutine(soundEndCheckCoroutine);
                soundEndCheckCoroutine = null;
            }

            if (currentSoundData != null)
            {
                currentSoundData.OnSoundFinished();
                currentSoundData = null;
            }
        }

        public bool IsPlaying()
        {
            return audioSource != null && audioSource.isPlaying;
        }

        public void SetVolume(float volume)
        {
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Clamp01(volume);
            }
        }

        public void SetPitch(float pitch)
        {
            if (audioSource != null)
            {
                audioSource.pitch = pitch;
            }
        }

        private IEnumerator CheckSoundEnd()
        {
            // Wait until the sound is no longer playing
            while (audioSource.isPlaying)
            {
                yield return null;
            }

            // Notify the sound data that playback has ended
            if (currentSoundData != null)
            {
                currentSoundData.OnSoundFinished();
                currentSoundData = null;
            }

            soundEndCheckCoroutine = null;
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
