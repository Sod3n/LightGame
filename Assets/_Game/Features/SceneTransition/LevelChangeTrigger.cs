using LightGame;
using LightGame.Features;
using LightGame.Events;
using LightGame.Globals;
using UnityEngine;

using LightGame.Core;
[RequireComponent(typeof(LightDetector))]
public class LevelChangeTrigger : MonoBehaviour
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

        UnityEngine.Debug.Log($"[TeleportDebug] LevelChangeTrigger.OnLightChangeEvent: isInLight={evt.IsInLight}, targetScene='{evt.TargetScene}'");
        OnInLightChange(evt.IsInLight, evt.TargetScene);
    }

    private void OnInLightChange(bool isInLight, string targetScene)
    {
        if (isInLight && !_wasInLevelChangeLight && targetScene != null)
        {
            // Entered LevelChange light - trigger scene change with the scene from the trigger
            TriggerLevelChange(targetScene);
        }
        else if (isInLight && targetScene == null)
        {
            UnityEngine.Debug.Log("[TeleportDebug] LevelChangeTrigger.OnInLightChange: entered light but targetScene is NULL, not triggering");
        }

        _wasInLevelChangeLight = isInLight;
    }

    private void TriggerLevelChange(string targetScene)
    {
        UnityEngine.Debug.Log($"[TeleportDebug] LevelChangeTrigger.TriggerLevelChange: publishing RequestLevelChangeEvent('{targetScene}')");
        // Publish event that LevelChangeView can listen to
        EventBus.Publish(new RequestLevelChangeEvent(targetScene));
    }
}
