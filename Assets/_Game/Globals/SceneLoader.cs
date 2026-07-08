using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Light_and_controller.Scripts
{
    public static class SceneLoader
    {
        private static string _currentSceneName = string.Empty;
        private static SceneName? _currentLevelScene = null;
        
        private static void InitializeScene(string sceneName)
        {
        }

        public static void LoadScene(SceneName sceneName)
        {
            SceneManager.LoadScene(sceneName.KeyToString(), LoadSceneMode.Additive);
            InitializeScene(sceneName.KeyToString());
        }
        
        /// <summary>
        /// Load a new level scene, unloading the previous level and ensuring Shared scene is loaded
        /// </summary>
        public static void LoadLevel(SceneName newLevel)
        {
            // Ensure Shared scene is loaded
            if (!IsSceneLoaded(SceneName.Shared))
            {
                SceneManager.LoadScene(SceneName.Shared.KeyToString(), LoadSceneMode.Additive);
            }
            
            // Unload previous level if exists
            if (_currentLevelScene.HasValue)
            {
                UnloadScene(_currentLevelScene.Value);
            }
            
            // Load new level
            var asyncOp = SceneManager.LoadSceneAsync(newLevel.KeyToString(), LoadSceneMode.Additive);
            
            // Set callback to set active scene when loaded
            if (asyncOp != null)
            {
                asyncOp.completed += (op) =>
                {
                    var scene = SceneManager.GetSceneByName(newLevel.KeyToString());
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        SceneManager.SetActiveScene(scene);
                    }
                };
            }
            
            // Update current level
            _currentLevelScene = newLevel;
            _currentSceneName = newLevel.KeyToString();
        }
        
        public static void UnloadScene(SceneName sceneName)
        {
            SceneManager.UnloadSceneAsync(sceneName.KeyToString());
        }

        public static bool IsSceneLoaded(SceneName sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName.KeyToString());
            Debug.Log($"Is scene '{sceneName}' loaded? {scene.IsValid() && scene.isLoaded}. Loaded scenes: {SceneManager.loadedSceneCount}");
            return scene.IsValid() && scene.isLoaded;
        }
        
        /// <summary>
        /// Try to parse a scene name string to SceneName enum
        /// </summary>
        private static bool TryParseSceneName(string sceneName, out SceneName result)
        {
            return System.Enum.TryParse(sceneName, true, out result);
        }
        
        /// <summary>
        /// Get the current level scene
        /// </summary>
        public static SceneName? GetCurrentLevel()
        {
            return _currentLevelScene;
        }
        
        public static void SetCurrentScene(string sceneName)
        {
            _currentSceneName = sceneName;
            if (TryParseSceneName(_currentSceneName, out SceneName parsedScene))
            {
                _currentLevelScene = parsedScene;
            }
        }
    }
}