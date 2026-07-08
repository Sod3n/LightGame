using System;
using LightGame.Globals;
using Unity.VisualScripting;
using UnityEngine;

using LightGame.Core;
namespace LightGame.Features
{
    [RequireComponent(typeof(LightDetector))]
    public class DangerLightSystem : MonoBehaviour, ILightable
    {
        [SerializeField] private DamageOverTimeEffect.EffectData damageOverTimeData;
        [SerializeField] private HealOverTimeEffect.EffectData healOverTimeData;

        private MonoBehaviour _lastEffect;

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
            OnInLightChange(evt.IsInLight);
        }

        public void OnInLightChange(bool isInLight)
        {
            if (isInLight) Heal();
            else Damage();
        }

        private void Heal()
        {
            Destroy(_lastEffect);
            var effect = gameObject.AddComponent<HealOverTimeEffect>();
            effect.Data = healOverTimeData;
            _lastEffect = effect;
        }

        private void Damage()
        {
            Destroy(_lastEffect);
            var effect = gameObject.AddComponent<DamageOverTimeEffect>();
            effect.Data = damageOverTimeData;
            _lastEffect = effect;
        }
    }
}