using System;
using Light_and_controller.Scripts.Events;
using Light_and_controller.Scripts.Systems;
using R3;
using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    public class PlayerHealthSystem : HealthSystem
    {
        private Rigidbody2D rigidbody = null;
        private AbilityManager abilityManager = null;

        protected override void Awake()
        {
            base.Awake();
            OnNearDeathStateChanged += HandleNearDeathStateChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnNearDeathStateChanged -= HandleNearDeathStateChanged;
        }

        private void HandleNearDeathStateChanged(bool isInNearDeath)
        {
            abilityManager ??= GetComponentInChildren<AbilityManager>();

            if (abilityManager == null)
            {
                Debug.LogWarning("AbilityManager not found in scene");
                return;
            }

            if (isInNearDeath)
            {
                abilityManager.DisableAllAbilities();
            }
            else
            {
                abilityManager.EnableAllAbilities();
            }
        }

        public override void Die()
        {
            rigidbody ??= GetComponent<Rigidbody2D>();
            rigidbody.linearVelocity = Vector2.zero;
            
            // Publish death event for PlayerDeathView to handle
            EventBus.Publish(new PlayerDiedEvent(gameObject));
        }

        public override void TakeDamage(int amount)
        {
            base.TakeDamage(amount);
            EventBus.Publish(new TakeDamageEvent { Amount = amount });
        }

        public class TakeDamageEvent
        {
            public int Amount;
        }
    }
}