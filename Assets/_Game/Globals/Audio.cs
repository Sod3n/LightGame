using LightGame.Features.Audio;
using UnityEngine;

namespace LightGame.Globals
{
    /// <summary>
    /// Audio façade. Mirrors Godot's <c>Audio</c> autoload (globals/audio.gd)
    /// — one call site for SFX and music, backed by the existing
    /// <see cref="SoundManager"/> pool implementation.
    /// </summary>
    public static class Audio
    {
        public static void Play(SoundData sound) => SoundManager.PlaySound(sound);
        public static void PlayAt(SoundData sound, Vector3 position) => SoundManager.PlaySoundAtPosition(sound, position);
        public static void Play2D(SoundData sound) => SoundManager.PlaySound2D(sound);
        public static void PlayMusic(SoundData music, float fadeIn = 0f) => SoundManager.PlayMusic(music, fadeIn);
        public static void StopMusic(float fadeOut = 0f) => SoundManager.StopMusic(fadeOut);
        public static void PauseMusic() => SoundManager.PauseMusic();
        public static void ResumeMusic() => SoundManager.ResumeMusic();
        public static void StopAll() => SoundManager.StopAllSounds();
        public static void SetMusicVolume(float v) => SoundManager.SetMusicVolume(v);
    }
}
