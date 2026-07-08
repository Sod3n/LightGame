using UnityEngine;

namespace LightGame.Globals
{
    /// <summary>
    /// Settings façade. Mirrors Godot's <c>Settings</c> autoload
    /// (globals/settings.gd). Thin wrapper over PlayerPrefs for now;
    /// evolve into a proper SettingsData model with property-changed
    /// signals when the settings UI is unified.
    /// </summary>
    public static class Settings
    {
        private const string MasterKey = "audio.master";
        private const string MusicKey = "audio.music";
        private const string SfxKey = "audio.sfx";
        private const string LocaleKey = "locale";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 1f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static string Locale
        {
            get => PlayerPrefs.GetString(LocaleKey, "en");
            set { PlayerPrefs.SetString(LocaleKey, value); PlayerPrefs.Save(); }
        }
    }
}
