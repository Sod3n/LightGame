using System.Collections.Generic;
using UnityEngine;

namespace LightGame.Globals
{
    [CreateAssetMenu(fileName = "LevelOrder", menuName = "Game/LevelOrder")]
    public class LevelOrder : ScriptableObject
    {
        public List<SceneReference> Levels = new();

        public int IndexOf(string sceneName)
        {
            if (sceneName == null) return -1;
            return Levels.FindIndex(l => l.SceneName == sceneName);
        }

        /// <summary>
        /// Get the next scene in the level order based on the current scene
        /// </summary>
        /// <param name="currentScene">The current scene's name</param>
        /// <returns>The next scene's name, or null if current scene is the last or not found</returns>
        public string GetNextScene(string currentScene)
        {
            int currentIndex = IndexOf(currentScene);

            // If current scene not found or is the last scene, return null
            if (currentIndex == -1 || currentIndex >= Levels.Count - 1)
            {
                return null;
            }

            return Levels[currentIndex + 1].SceneName;
        }

        /// <summary>
        /// Get the next scene based on the currently active scene
        /// </summary>
        /// <returns>The next scene's name, or null if current scene is the last or not found</returns>
        public string GetNextScene()
        {
            var currentLevel = SceneLoader.GetCurrentLevel();
            return currentLevel == null ? null : GetNextScene(currentLevel);
        }
    }
}
