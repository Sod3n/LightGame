using LightGame.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightGame.Globals
{
    public static class SceneLoader
    {
        private static string _currentLevelScene = null;

        public static void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Load a new level scene, unloading the previous level and ensuring Shared scene is loaded
        /// </summary>
        public static void LoadLevel(string newLevel)
        {
            // Ensure Shared scene is loaded
            if (!IsSceneLoaded("Shared"))
            {
                SceneManager.LoadScene("Shared", LoadSceneMode.Additive);
            }

            // Unload previous level if exists
            if (_currentLevelScene != null)
            {
                UnloadScene(_currentLevelScene);
            }

            // Load new level
            var asyncOp = SceneManager.LoadSceneAsync(newLevel, LoadSceneMode.Additive);

            // Set callback to set active scene when loaded
            if (asyncOp != null)
            {
                asyncOp.completed += (op) =>
                {
                    var scene = SceneManager.GetSceneByName(newLevel);
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        SceneManager.SetActiveScene(scene);
                    }
                };
            }

            // Update current level
            _currentLevelScene = newLevel;
        }

        public static void UnloadScene(string sceneName)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }

        public static bool IsSceneLoaded(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        /// <summary>
        /// Get the current level scene's name
        /// </summary>
        public static string GetCurrentLevel()
        {
            return _currentLevelScene;
        }

        public static void SetCurrentScene(string sceneName)
        {
            _currentLevelScene = sceneName;
        }
    }
}
