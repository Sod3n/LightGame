using LightGame;

using LightGame.Globals;
namespace LightGame.Events
{
    /// <summary>
    /// Event to request a level change
    /// </summary>
    public class RequestLevelChangeEvent
    {
        public string TargetScene { get; set; }

        public RequestLevelChangeEvent(string targetScene)
        {
            TargetScene = targetScene;
        }
    }
}
