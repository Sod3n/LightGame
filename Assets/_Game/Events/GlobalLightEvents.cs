using DG.Tweening;
using Light_and_controller.Scripts.Systems;

public enum LightType
{
    Default,
    LevelChange
}

namespace Light_and_controller.Scripts.Events
{
    /// <summary>
    /// Event to set intensity with tweening
    /// </summary>
    public class SetLightIntensityEvent
    {
        public float TargetIntensity { get; set; }
        public float Duration { get; set; }
        public Ease Ease { get; set; }
        public LightType? LightType { get; set; }

        public SetLightIntensityEvent(float targetIntensity, float duration = 0f, Ease ease = Ease.Unset, LightType? lightType = null)
        {
            TargetIntensity = targetIntensity;
            Duration = duration;
            Ease = ease;
            LightType = lightType;
        }
    }

    /// <summary>
    /// Event to set intensity immediately without tweening
    /// </summary>
    public class SetLightIntensityImmediateEvent
    {
        public float TargetIntensity { get; set; }
        public LightType? LightType { get; set; }

        public SetLightIntensityImmediateEvent(float targetIntensity, LightType? lightType = null)
        {
            TargetIntensity = targetIntensity;
            LightType = lightType;
        }
    }

    /// <summary>
    /// Event to restore original intensity
    /// </summary>
    public class RestoreLightIntensityEvent
    {
        public float Duration { get; set; }
        public Ease Ease { get; set; }
        public LightType? LightType { get; set; }

        public RestoreLightIntensityEvent(float duration = 0f, Ease ease = Ease.Unset, LightType? lightType = null)
        {
            Duration = duration;
            Ease = ease;
            LightType = lightType;
        }
    }

    /// <summary>
    /// Event to fade to black (intensity 0)
    /// </summary>
    public class FadeToBlackEvent
    {
        public float Duration { get; set; }
        public Ease Ease { get; set; }
        public LightType? LightType { get; set; }

        public FadeToBlackEvent(float duration = 0f, Ease ease = Ease.Unset, LightType? lightType = null)
        {
            Duration = duration;
            Ease = ease;
            LightType = lightType;
        }
    }

    /// <summary>
    /// Event to fade to full brightness (intensity 1)
    /// </summary>
    public class FadeToFullEvent
    {
        public float Duration { get; set; }
        public Ease Ease { get; set; }
        public LightType? LightType { get; set; }

        public FadeToFullEvent(float duration = 0f, Ease ease = Ease.Unset, LightType? lightType = null)
        {
            Duration = duration;
            Ease = ease;
            LightType = lightType;
        }
    }

    /// <summary>
    /// Helper class for publishing light control events through the EventBus
    /// </summary>
    public static class GlobalLightEvents
    {
        /// <summary>
        /// Set intensity for all lights with tweening
        /// </summary>
        public static void SetIntensity(float targetIntensity, float duration = 0f, Ease ease = Ease.Unset, LightType? lightType = null)
        {
            EventBus.Publish(new SetLightIntensityEvent(targetIntensity, duration, ease, lightType));
        }

        /// <summary>
        /// Set intensity immediately without tweening
        /// </summary>
        public static void SetIntensityImmediate(float targetIntensity, LightType? lightType = null)
        {
            EventBus.Publish(new SetLightIntensityImmediateEvent(targetIntensity, lightType));
        }

        /// <summary>
        /// Restore all lights to their original intensities with tweening
        /// </summary>
        public static void RestoreOriginalIntensities(float duration = 0f, Ease ease = Ease.Unset, LightType? lightType = null)
        {
            EventBus.Publish(new RestoreLightIntensityEvent(duration, ease, lightType));
        }

        /// <summary>
        /// Fade all lights to black (intensity 0)
        /// </summary>
        public static void FadeToBlack(float duration = 0f, Ease ease = Ease.Unset, LightType? lightType = null)
        {
            EventBus.Publish(new FadeToBlackEvent(duration, ease, lightType));
        }

        /// <summary>
        /// Fade all lights to full brightness (intensity 1)
        /// </summary>
        public static void FadeToFull(float duration = 0f, Ease ease = Ease.Unset, LightType? lightType = null)
        {
            EventBus.Publish(new FadeToFullEvent(duration, ease, lightType));
        }
    }
}
