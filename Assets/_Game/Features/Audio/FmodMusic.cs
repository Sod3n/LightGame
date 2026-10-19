using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace LightGame.Features.Audio
{
    /// <summary>
    /// Music and ambience from the FMOD banks. One music instance runs across scene loads;
    /// its "Main Menu" parameter switches between the menu and the in-level arrangement.
    /// </summary>
    public static class FmodMusic
    {
        private const string MusicPath = "event:/Music/Music";
        private const string AtmoPath = "event:/Sound Design/Atmo";
        private const string MainMenuParameter = "Main Menu";

        private static EventInstance _music;
        private static EventInstance _atmo;

        public static void EnterScene(bool mainMenu)
        {
            Start(ref _music, MusicPath);
            SetParameter(_music, MainMenuParameter, mainMenu ? 1f : 0f);

            if (mainMenu) Stop(ref _atmo);
            else Start(ref _atmo, AtmoPath);
        }

        private static void Start(ref EventInstance instance, string path)
        {
            if (instance.isValid()) return;
            instance = RuntimeManager.CreateInstance(path);
            instance.start();
        }

        private static void Stop(ref EventInstance instance)
        {
            if (!instance.isValid()) return;
            instance.stop(STOP_MODE.ALLOWFADEOUT);
            instance.release();
            instance.clearHandle();
        }

        // The parameter may be local to the event or global, depending on the FMOD project.
        private static void SetParameter(EventInstance instance, string name, float value)
        {
            if (instance.setParameterByName(name, value) == FMOD.RESULT.OK) return;
            var result = RuntimeManager.StudioSystem.setParameterByName(name, value);
            if (result != FMOD.RESULT.OK) Debug.LogWarning($"FMOD parameter '{name}' not set: {result}");
        }
    }
}
