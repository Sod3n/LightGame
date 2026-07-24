using System;
using LightGame.Features.Audio;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using LightGame.Core;
using LightGame.Globals;
namespace LightGame.Features
{
    public class SceneRoot : MonoBehaviour
    {
        public static SceneRoot Instance { get; private set; }
        

        private void Awake()
        {
            Instance = this;

            // Set current scene in SceneLoader based on which scene this SceneRoot belongs to
            SceneLoader.SetCurrentScene(gameObject.scene.name);
            
            if (!SceneLoader.IsSceneLoaded("Shared"))
            {
                SceneManager.LoadScene("Shared", LoadSceneMode.Additive);
            }

            Game.Init();
            ExecuteEvents.ExecuteHierarchy<IInitializable>(gameObject, null, (x, _) => x.Initialize());
            var mainTheme = Addressables.LoadAssetAsync<SoundData>("Sounds/MainTheme").WaitForCompletion();
            if(!SoundManager.IsMusicPlaying()) SoundManager.PlayMusic(mainTheme, 2f);
        }
    }
}