using Light_and_controller.Scripts.Systems;
using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    [RequireComponent(typeof(LightDetector))]
    public class HealInLightSystem : MonoBehaviourWithData<HealOverTimeEffect.EffectData>, ILightable
    {
        private MonoBehaviour _currentEffect;
        
        private void OnEnable()
        {
            EventBus.Subscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
        }
        
        private void OnDisable()
        {
            EventBus.Unsubscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
            StopHealing();
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
                StartHealing();
            }
            else
            {
                StopHealing();
            }
        }
        
        private void StartHealing()
        {
            StopHealing();
            var effect = gameObject.AddComponent<HealOverTimeEffect>();
            effect.Data = Data;
            _currentEffect = effect;
        }
        
        private void StopHealing()
        {
            if (_currentEffect != null)
            {
                Destroy(_currentEffect);
                _currentEffect = null;
            }
        }
    }
}
