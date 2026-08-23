using LightGame.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightGame.Globals
{
    public static class SceneLoader
    {
        private static string _currentLevelScene = null;
        private static string _currentLevelSceneBeforeSwap = null;

        public static void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Load a new level scene, unloading the previous level and ensuring Shared scene is loaded
        /// </summary>
        public static void LoadLevel(string newLevel)
        {
            Debug.Log($"[TeleportDebug] SceneLoader.LoadLevel('{newLevel}') called, previous currentLevelScene='{_currentLevelScene}'");

            // Ensure Shared scene is loaded
            if (!IsSceneLoaded("Shared"))
            {
                SceneManager.LoadScene("Shared", LoadSceneMode.Additive);
            }

            // Unload previous level if exists
            if (_currentLevelScene != null)
            {
                Debug.Log($"[TeleportDebug] SceneLoader.LoadLevel: unloading '{_currentLevelScene}' (async, not awaited) at frame={Time.frameCount}");
                UnloadScene(_currentLevelScene);
            }

            // Load new level
            var asyncOp = SceneManager.LoadSceneAsync(newLevel, LoadSceneMode.Additive);
            Debug.Log($"[TeleportDebug] SceneLoader.LoadLevel: started LoadSceneAsync('{newLevel}') at frame={Time.frameCount}");
            if (asyncOp == null)
            {
                Debug.LogError($"[TeleportDebug] SceneLoader.LoadLevel: LoadSceneAsync('{newLevel}') returned NULL — scene is not in Build Settings or name is wrong!");
            }

            // Set callback to set active scene when loaded
            if (asyncOp != null)
            {
                asyncOp.completed += (op) =>
                {
                    var scene = SceneManager.GetSceneByName(newLevel);
                    var oldScene = SceneManager.GetSceneByName(_currentLevelSceneBeforeSwap);
                    var players = Object.FindObjectsByType<PlayerMain>(FindObjectsSortMode.None);
                    Debug.Log($"[TeleportDebug] SceneLoader.LoadLevel: async load of '{newLevel}' completed at frame={Time.frameCount}, valid={scene.IsValid()}, loaded={scene.isLoaded}, oldScene('{_currentLevelSceneBeforeSwap}')StillLoaded={oldScene.IsValid() && oldScene.isLoaded}, PlayerMain count in memory={players.Length}");
                    foreach (var p in players)
                        Debug.Log($"[TeleportDebug]   PlayerMain instanceID={p.GetInstanceID()}, position={p.transform.position}, scene='{p.gameObject.scene.name}'");
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        SceneManager.SetActiveScene(scene);
                    }
                };
            }
            _currentLevelSceneBeforeSwap = _currentLevelScene;

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
