using UnityEngine;
using UnityEngine.SceneManagement;

namespace Light_and_controller.Scripts
{
    public static class SaveSystem
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