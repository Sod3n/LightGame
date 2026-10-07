using LightGame.Globals;
using UnityEngine;

using LightGame.Core;
namespace LightGame.Features
{
    /// <summary>
    /// Applies a heal-over-time effect while in light and/or a damage-over-time effect while in dark.
    /// Leave either Amount at 0 to disable that side.
    ///
    /// Replaces (and unifies) the earlier <c>HealInLightSystem</c>, <c>DamageInDarkSystem</c>,
    /// and <c>DangerLightSystem</c>.
    /// </summary>
    [RequireComponent(typeof(LightDetector))]
    public class LightReactiveEffect : MonoBehaviour
    {
        [Header("In Light (heal). Leave Amount = 0 to disable.")]
        [SerializeField] private HealOverTimeEffect.EffectData onLight;

        [Header("In Dark (damage). Leave Amount = 0 to disable.")]
        [SerializeField] private DamageOverTimeEffect.EffectData onDark;

        private MonoBehaviour _current;

        public void SetOnLight(HealOverTimeEffect.EffectData data)
        {
            onLight = data;
            ApplyCurrentLight();
        }

        public void SetOnDark(DamageOverTimeEffect.EffectData data)
        {
            onDark = data;
            ApplyCurrentLight();
        }

        public bool AnyActive => (onLight != null && onLight.Amount > 0) || (onDark != null && onDark.Amount > 0);

        private void OnEnable()
        {
            EventBus.Subscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
            ApplyCurrentLight();
        }

        // LightDetector only publishes changes, so a target that starts in the dark never gets a first event.
        private void ApplyCurrentLight()
        {
            if (isActiveAndEnabled) OnInLightChange(GetComponent<LightDetector>().IsInLightOfType(LightType.Default));
        }
        private void OnDisable()
        {
            EventBus.Unsubscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
            ClearEffect();
        }

        private void OnLightChangeEvent(LightChangeEvent evt)
        {
            if (evt.LightType.HasValue && evt.LightType.Value != LightType.Default) return;
            OnInLightChange(evt.IsInLight);
        }

        public void OnInLightChange(bool isInLight)
        {
            ClearEffect();
            if (isInLight && onLight != null && onLight.Amount > 0)
            {
                var effect = gameObject.AddComponent<HealOverTimeEffect>();
                effect.Data = onLight;
                _current = effect;
            }
            else if (!isInLight && onDark != null && onDark.Amount > 0)
            {
                var effect = gameObject.AddComponent<DamageOverTimeEffect>();
                effect.Data = onDark;
                _current = effect;
            }
        }

        private void ClearEffect()
        {
            if (_current != null) Destroy(_current);
            _current = null;
        }
    }
}
