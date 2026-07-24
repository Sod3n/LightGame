using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using LightGame.Globals;
namespace LightGame.Features.UI
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private void OnEnable()
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            newGameButton.onClick.AddListener(OnNewGameButtonClicked);
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
        
        private void OnDisable()
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
            newGameButton.onClick.RemoveListener(OnNewGameButtonClicked);
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }


        private void OnContinueButtonClicked()
        {
            Debug.Log("Continue button clicked");
        }

        private void OnNewGameButtonClicked()
        {
            Game.Init();
            
            if (Game.LevelOrder != null && Game.LevelOrder.Levels.Count > 0)
            {
                var firstLevel = Game.LevelOrder.Levels[0].SceneName;
                SceneLoader.LoadLevel(firstLevel);
                SceneLoader.UnloadScene("MainMenu");
            }
            else
            {
                Debug.LogError("LevelOrder is not configured or empty!");
            }
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("Settings button clicked");
        }

        private void OnQuitButtonClicked()
        {
            #if UNITY_EDITOR
            if (Application.isEditor)
            {
                // Stop the editor
                EditorApplication.isPlaying = false;
                return;
            }
            #endif
            Application.Quit();
        }
    }
}