using System.Collections;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

[RequireComponent(typeof(LightDetector))]
public class PlayerTeleportSystem : MonoBehaviourWithData<PlayerTeleportSkill.TeleportData>, ILightable
{
    [SerializeField] private float disableDelay = 0.5f; // Delay in seconds before ability is removed

    private PlayerTeleportSkill TeleportComponent;
    private Coroutine disableCoroutine;

    private void Awake()
    {
        // Pre-create the component but keep it disabled
        TeleportComponent = gameObject.GetComponent<PlayerTeleportSkill>();
        if (TeleportComponent == null)
        {
            TeleportComponent = gameObject.AddComponent<PlayerTeleportSkill>();
        }
        TeleportComponent.Data = Data;
        TeleportComponent.enabled = false;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);

        // Clean up any running coroutine
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }
    }

    private void OnLightChangeEvent(LightChangeEvent evt)
    {
        // Only respond to Default light type (ignore LevelChange lights)
        if (evt.LightType.HasValue && evt.LightType.Value != LightType.Default)
            return;
            
        OnInLightChange(evt.IsInLight);
    }

    public void OnInLightChange(bool IsInLight)
    {
        if (IsInLight) TeleportOn();
        else TeleportOff();
    }

    public void TeleportOn()
    {
        // Cancel any pending disable coroutine
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }

        if (TeleportComponent != null)
        {
            TeleportComponent.enabled = true;
        }
    }

    public void TeleportOff()
    {
        // Cancel any existing disable coroutine
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
        }

        // Start delayed disable
        disableCoroutine = StartCoroutine(DisableAfterDelay());
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);

        if (TeleportComponent != null)
        {
            TeleportComponent.enabled = false;
        }

        disableCoroutine = null;
    }
}
