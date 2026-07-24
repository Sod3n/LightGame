using LightGame;

using LightGame.Globals;
namespace LightGame.Core
{
    public class LightChangeEvent
    {
        public bool IsInLight { get; set; }
        public LightType? LightType { get; set; }
        public string TargetScene { get; set; }

        public LightChangeEvent(bool isInLight, LightType? lightType = null, string targetScene = null)
        {
            IsInLight = isInLight;
            LightType = lightType;
            TargetScene = targetScene;
        }
    }
}
