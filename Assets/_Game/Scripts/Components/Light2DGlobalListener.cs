using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Light_and_controller.Scripts.Events;
using Light_and_controller.Scripts.Systems;

[RequireComponent(typeof(Light2D))]
public class Light2DGlobalListener : MonoBehaviour
{
    // Static storage for light states across scene changes
    private static float _storedIntensity = -1f;
    
    [Header("Light Type")]
    [SerializeField] private LightType lightType = LightType.Default;
    
    [Header("Tween Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutQuad;
    
    [Header("Auto Register")]
    [SerializeField] private bool autoRegisterOnEnable = true;
    
    private Light2D light2D;
    private float originalIntensity;
    private Tween currentTween;
    
    public LightType Type => lightType;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();

        originalIntensity = light2D.intensity;
        
        // If there's a stored intensity from a scene change, apply it immediately
        if (_storedIntensity >= 0f)
        {
            light2D.intensity = _storedIntensity * originalIntensity;
        }
    }

    private void OnEnable()
    {
        if (autoRegisterOnEnable)
        {
            EventBus.Subscribe<SetLightIntensityEvent>(HandleSetIntensity);
            EventBus.Subscribe<SetLightIntensityImmediateEvent>(HandleSetIntensityImmediate);
            EventBus.Subscribe<RestoreLightIntensityEvent>(HandleRestoreOriginal);
            EventBus.Subscribe<FadeToBlackEvent>(HandleFadeToBlack);
            EventBus.Subscribe<FadeToFullEvent>(HandleFadeToFull);
        }
    }

    private void OnDisable()
    {
        if (autoRegisterOnEnable)
        {
            EventBus.Unsubscribe<SetLightIntensityEvent>(HandleSetIntensity);
            EventBus.Unsubscribe<SetLightIntensityImmediateEvent>(HandleSetIntensityImmediate);
            EventBus.Unsubscribe<RestoreLightIntensityEvent>(HandleRestoreOriginal);
            EventBus.Unsubscribe<FadeToBlackEvent>(HandleFadeToBlack);
            EventBus.Unsubscribe<FadeToFullEvent>(HandleFadeToFull);
        }
        
        currentTween?.Kill();
    }

    private void HandleSetIntensity(SetLightIntensityEvent evt)
    {
        // Only respond if light type matches or no type specified
        if (evt.LightType.HasValue && evt.LightType.Value != lightType)
            return;
            
        currentTween?.Kill();
        
        float tweenDuration = evt.Duration > 0 ? evt.Duration : duration;
        Ease tweenEase = evt.Ease != Ease.Unset ? evt.Ease : ease;
        
        currentTween = DOVirtual.Float(light2D.intensity, evt.TargetIntensity, tweenDuration, 
            value => light2D.intensity = value).SetEase(tweenEase);
    }

    private void HandleSetIntensityImmediate(SetLightIntensityImmediateEvent evt)
    {
        // Only respond if light type matches or no type specified
        if (evt.LightType.HasValue && evt.LightType.Value != lightType)
            return;
            
        currentTween?.Kill();
        light2D.intensity = evt.TargetIntensity;
    }

    private void HandleRestoreOriginal(RestoreLightIntensityEvent evt)
    {
        currentTween?.Kill();
        
        float tweenDuration = evt.Duration > 0 ? evt.Duration : duration;
        Ease tweenEase = evt.Ease != Ease.Unset ? evt.Ease : ease;
        
        currentTween = DOVirtual.Float(light2D.intensity, originalIntensity, tweenDuration, 
            value => light2D.intensity = value).SetEase(tweenEase);
    }

    private void HandleFadeToBlack(FadeToBlackEvent evt)
    {
        // Only respond if light type matches or no type specified
        if (evt.LightType.HasValue && evt.LightType.Value != lightType)
            return;
            
        currentTween?.Kill();
        
        float tweenDuration = evt.Duration > 0 ? evt.Duration : duration;
        Ease tweenEase = evt.Ease != Ease.Unset ? evt.Ease : ease;
        
        // Fade to 0 (black)
        currentTween = DOVirtual.Float(light2D.intensity, 0f, tweenDuration, 
            value => light2D.intensity = value).SetEase(tweenEase);
    }

    private void HandleFadeToFull(FadeToFullEvent evt)
    {
        // Only respond if light type matches or no type specified
        if (evt.LightType.HasValue && evt.LightType.Value != lightType)
            return;
            
        currentTween?.Kill();
        
        float tweenDuration = evt.Duration > 0 ? evt.Duration : duration;
        Ease tweenEase = evt.Ease != Ease.Unset ? evt.Ease : ease;
        
        // Fade to original intensity (not 1.0, but the light's original value)
        currentTween = DOVirtual.Float(light2D.intensity, originalIntensity, tweenDuration, 
            value => light2D.intensity = value).SetEase(tweenEase);
    }

    /// <summary>
    /// Update the stored original intensity to the current value
    /// </summary>
    public void UpdateOriginalIntensity()
    {
        originalIntensity = light2D.intensity;
    }

    /// <summary>
    /// Set a new original intensity value
    /// </summary>
    public void SetOriginalIntensity(float intensity)
    {
        originalIntensity = intensity;
    }
    
    /// <summary>
    /// Store the current intensity in static storage for scene transitions
    /// </summary>
    public static void StoreCurrentIntensity(float intensity)
    {
        _storedIntensity = intensity;
    }
    
    /// <summary>
    /// Clear the stored intensity
    /// </summary>
    public static void ClearStoredIntensity()
    {
        _storedIntensity = -1f;
    }

    /// <summary>
    /// Fade the light to black (intensity 0) with optional callback
    /// </summary>
    /// <param name="fadeDuration">Duration of the fade animation</param>
    /// <param name="onComplete">Optional callback when fade completes</param>
    public void FadeOut(float fadeDuration = 0.5f, System.Action onComplete = null)
    {
        currentTween?.Kill();

        currentTween = DOVirtual.Float(light2D.intensity, 0f, fadeDuration,
            value => light2D.intensity = value)
            .SetEase(ease)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// Fade the light to its original intensity with optional callback
    /// </summary>
    /// <param name="fadeDuration">Duration of the fade animation</param>
    /// <param name="onComplete">Optional callback when fade completes</param>
    public void FadeIn(float fadeDuration = 0.5f, System.Action onComplete = null)
    {
        currentTween?.Kill();

        currentTween = DOVirtual.Float(light2D.intensity, originalIntensity, fadeDuration,
            value => light2D.intensity = value)
            .SetEase(ease)
            .OnComplete(() => onComplete?.Invoke());
    }
}
