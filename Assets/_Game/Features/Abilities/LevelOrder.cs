using System.Collections.Generic;
using UnityEngine;

namespace Light_and_controller.Scripts
{
    [CreateAssetMenu(fileName = "LevelOrder", menuName = "Game/LevelOrder")]
    public class LevelOrder : ScriptableObject
    {
        public List<SceneName> Value = new();

        /// <summary>
        /// Get the next scene in the level order based on the current scene
        /// </summary>
        /// <param name="currentScene">The current scene</param>
        /// <returns>The next scene, or null if current scene is the last or not found</returns>
        public SceneName? GetNextScene(SceneName currentScene)
        {
            int currentIndex = Value.IndexOf(currentScene);

            // If current scene not found or is the last scene, return null
            if (currentIndex == -1 || currentIndex >= Value.Count - 1)
            {
                return null;
            }

            return Value[currentIndex + 1];
        }

        /// <summary>
        /// Get the next scene based on the currently active scene
        /// </summary>
        /// <returns>The next scene, or null if current scene is the last or not found</returns>
        public SceneName? GetNextScene()
        {
            var currentLevel = SceneLoader.GetCurrentLevel();

            if (!currentLevel.HasValue)
            {
                return null;
            }

            return GetNextScene(currentLevel.Value);
        }
    }
}