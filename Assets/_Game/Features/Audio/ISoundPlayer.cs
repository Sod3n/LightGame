using UnityEngine;

namespace LightGame.Audio
{
    /// <summary>
    /// Interface for sound player implementations.
    /// This abstraction allows easy migration from Unity's AudioSource to FMOD or other audio systems.
    /// </summary>
    public interface ISoundPlayer
    {
        /// <summary>
        /// Plays a sound with the given sound data.
        /// </summary>
        /// <param name="soundData">The sound data to play</param>
        /// <param name="position">Optional world position for 3D sounds</param>
        void Play(SoundData soundData, Vector3? position = null);

        /// <summary>
        /// Stops the currently playing sound.
        /// </summary>
        void Stop();

        /// <summary>
        /// Checks if a sound is currently playing.
        /// </summary>
        /// <returns>True if playing, false otherwise</returns>
        bool IsPlaying();

        /// <summary>
        /// Sets the volume of the sound player.
        /// </summary>
        /// <param name="volume">Volume from 0 to 1</param>
        void SetVolume(float volume);

        /// <summary>
        /// Sets the pitch of the sound player.
        /// </summary>
        /// <param name="pitch">Pitch value</param>
        void SetPitch(float pitch);
    }
}
