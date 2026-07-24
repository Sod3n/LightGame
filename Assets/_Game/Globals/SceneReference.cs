using UnityEngine;

namespace LightGame.Globals
{
    [System.Serializable]
    public class SceneReference
    {
        [SerializeField] private Object sceneAsset;
        [SerializeField] private string sceneName;

        public string SceneName => sceneName;
    }
}
