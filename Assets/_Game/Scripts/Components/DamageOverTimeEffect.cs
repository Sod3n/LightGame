using System;
using System.Collections;
using Light_and_controller.Scripts.Events;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    public class DamageOverTimeEffect : MonoBehaviourEffect<DamageOverTimeEffect.EffectData>
    {
        [Serializable]
        public class EffectData : Components.EffectData
        {
            public int Amount;
            public bool ResetTickOnHealthChange = true;
        }

        private PendingDamageEvent _pendingEvent;
        private bool _subscribedToHealthChanges;

        private void OnDisable()
        {
            if (_subscribedToHealthChanges)
            {
                EventBus.Unsubscribe<DamageEvent>(gameObject, OnAnyHealthChange);
                EventBus.Unsubscribe<HealEvent>(gameObject, OnAnyHealthChange);
                _subscribedToHealthChanges = false;
            }
        }

        private void OnAnyHealthChange(DamageEvent evt) => ResetTick();
        private void OnAnyHealthChange(HealEvent evt) => ResetTick();

        private void ResetTick()
        {
            _timeSinceLastTick = 0f;
            // Cancel and republish pending event with new timing
            _pendingEvent?.Cancel?.Invoke();
            PublishPendingEvent();
        }

        protected override void OnStart()
        {
            // Subscribe to health changes if enabled
            if (Data.ResetTickOnHealthChange)
            {
                EventBus.Subscribe<DamageEvent>(gameObject, OnAnyHealthChange);
                EventBus.Subscribe<HealEvent>(gameObject, OnAnyHealthChange);
                _subscribedToHealthChanges = true;
            }

            // Publish first pending event
            PublishPendingEvent();
        }
        
        protected override void Tick()
        {
            _pendingEvent = null;

            // Check if applying damage would kill the player
            var healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                // Only apply damage if it won't reduce health below 1
                int damageToApply = Data.Amount;
                if (healthSystem.Health - damageToApply < 1)
                {
                    damageToApply = healthSystem.Health - 1;
                }

                // Only apply damage if there's damage to apply
                if (damageToApply > 0)
                {
                    EventBus.Publish(gameObject, new DamageEvent { Amount = damageToApply });
                }
            }
            else
            {
                // If no health system, apply damage normally
                EventBus.Publish(gameObject, new DamageEvent { Amount = Data.Amount });
            }

            // Publish next pending event if not finished
            if (Data.IsInfinity || _currentTime + Data.Rate < Data.Duration)
            {
                PublishPendingEvent();
            }
        }

        private void PublishPendingEvent()
        {
            _pendingEvent = new PendingDamageEvent
            {
                Amount = Data.Amount,
                Duration = Data.Rate
            };
            EventBus.Publish(gameObject, _pendingEvent);
        }

        private void OnDestroy()
        {
            // Cancel pending event if effect is destroyed before completion
            _pendingEvent?.Cancel?.Invoke();
        }
    }
}