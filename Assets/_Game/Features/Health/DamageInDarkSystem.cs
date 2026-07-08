using Light_and_controller.Scripts.Systems;
using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    [RequireComponent(typeof(LightDetector))]
    public class DamageInDarkSystem : MonoBehaviourWithData<DamageOverTimeEffect.EffectData>, ILightable
    {
        private MonoBehaviour _currentEffect;
        
        private void OnEnable()
        {
            EventBus.Subscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
        }
        
        private void OnDisable()
        {
            EventBus.Unsubscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
            StopDamage();
        }
        
        private void OnLightChangeEvent(LightChangeEvent evt)
        {
            // Only respond to Default light type (ignore LevelChange lights)
            if (evt.LightType.HasValue && evt.LightType.Value != LightType.Default)
                return;
                
            OnInLightChange(evt.IsInLight);
        }
        
        public void OnInLightChange(bool isInLight)
        {
            if (isInLight)
            {
                StopDamage();
            }
            else
            {
                StartDamage();
            }
        }
        
        private void StartDamage()
        {
            StopDamage();
            var effect = gameObject.AddComponent<DamageOverTimeEffect>();
            effect.Data = Data;
            _currentEffect = effect;
        }
        
        private void StopDamage()
        {
            if (_currentEffect != null)
            {
                Destroy(_currentEffect);
                _currentEffect = null;
            }
        }
    }
}
