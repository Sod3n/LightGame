using System;
using LightGame.Audio;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Light_and_controller.Scripts.Components
{
    public class SceneRoot : MonoBehaviour
    {
        public static SceneRoot Instance { get; private set; }
        

        private void Awake()
        {
            Instance = this;

            // Set current scene in SceneLoader based on which scene this SceneRoot belongs to
            SceneLoader.SetCurrentScene(gameObject.scene.name);
            
            if (!SceneLoader.IsSceneLoaded(SceneName.Shared))
            {
                SceneManager.LoadScene(SceneName.Shared.KeyToString(), LoadSceneMode.Additive);
            }

            GD.Init();
            ExecuteEvents.ExecuteHierarchy<IInitializable>(gameObject, null, (x, _) => x.Initialize());
            var mainTheme = Addressables.LoadAssetAsync<SoundData>("Sounds/MainTheme").WaitForCompletion();
            if(!SoundManager.IsMusicPlaying()) SoundManager.PlayMusic(mainTheme, 2f);
        }
    }
}