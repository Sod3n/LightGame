using LightGame;

namespace LightGame.Events
{
    /// <summary>
    /// Event to request a level change
    /// </summary>
    public class RequestLevelChangeEvent
    {
        public SceneName TargetScene { get; set; }

        public RequestLevelChangeEvent(SceneName targetScene)
        {
            TargetScene = targetScene;
        }
    }
}
