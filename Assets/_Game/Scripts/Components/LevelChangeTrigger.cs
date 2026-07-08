using Light_and_controller.Scripts;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Events;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

[RequireComponent(typeof(LightDetector))]
public class LevelChangeTrigger : MonoBehaviour, ILightable
{
    private LightDetector _lightDetector;
    private bool _wasInLevelChangeLight = false;

    private void Awake()
    {
        _lightDetector = GetComponent<LightDetector>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
    }

    private void OnLightChangeEvent(LightChangeEvent evt)
    {
        // Only respond to LevelChange light type
        if (!evt.LightType.HasValue || evt.LightType.Value != LightType.LevelChange)
            return;

        OnInLightChange(evt.IsInLight, evt.TargetScene);
    }

    public void OnInLightChange(bool isInLight)
    {
        // This method is kept for ILightable interface compatibility
        // but the actual logic is in the overload below
    }

    private void OnInLightChange(bool isInLight, SceneName? targetScene)
    {
        if (isInLight && !_wasInLevelChangeLight && targetScene.HasValue)
        {
            // Entered LevelChange light - trigger scene change with the scene from the trigger
            TriggerLevelChange(targetScene.Value);
        }
        
        _wasInLevelChangeLight = isInLight;
    }

    private void TriggerLevelChange(SceneName targetScene)
    {
        // Publish event that LevelChangeView can listen to
        EventBus.Publish(new RequestLevelChangeEvent(targetScene));
    }
}
