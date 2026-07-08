using Light_and_controller.Scripts;

namespace Light_and_controller.Scripts.Components
{
    public interface ILightable
    {
        public void OnInLightChange(bool isInLight);
    }

    public class LightChangeEvent
    {
        public bool IsInLight { get; set; }
        public LightType? LightType { get; set; }
        public SceneName? TargetScene { get; set; }

        public LightChangeEvent(bool isInLight, LightType? lightType = null, SceneName? targetScene = null)
        {
            IsInLight = isInLight;
            LightType = lightType;
            TargetScene = targetScene;
        }
    }
}