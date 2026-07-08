using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightGame.Globals
{
    public static class SaveManager
    {
        public static void SaveGame()
        {
            PlayerPrefs.SetInt("Scene", SceneManager.GetActiveScene().buildIndex); // Сохраняем индекс активной сцены
        }

        public static void LoadGame()
        {
            SceneManager.LoadScene(PlayerPrefs.GetInt("scene"));
        }
    }
}