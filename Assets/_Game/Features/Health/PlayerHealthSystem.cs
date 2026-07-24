using System;
using LightGame.Events;
using LightGame.Globals;
using UnityEngine;

namespace LightGame.Features
{
    public class PlayerHealthSystem : HealthSystem
    {
        private Rigidbody2D rigidbody = null;
        private AbilityManager abilityManager = null;
        private PlayerMain playerMain = null;

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

            playerMain ??= GetComponent<PlayerMain>();
            if (playerMain != null)
            {
                // Play the death animation first; only fade/respawn once it has finished.
                playerMain.PlayDeath(() => EventBus.Publish(new PlayerDiedEvent(gameObject)));
            }
            else
            {
                // No player animator available - fall back to immediate death handling.
                EventBus.Publish(new PlayerDiedEvent(gameObject));
            }
        }

        public override void Heal(int amount)
        {
            bool wasDead = Health <= 0;
            base.Heal(amount);

            // Revived from death (e.g. checkpoint respawn heals to full): release the
            // death lock and hand control back to the movement state machine.
            if (wasDead && Health > 0)
            {
                playerMain ??= GetComponent<PlayerMain>();
                playerMain?.OnRespawn();
            }
        }

        public override void TakeDamage(int amount)
        {
            int healthBefore = Health;
            base.TakeDamage(amount); // may call Die() internally when health reaches 0

            // Play the hit reaction only when damage actually landed (not blocked by
            // invincibility) and it wasn't fatal - Die() already plays the death anim.
            if (Health > 0 && Health < healthBefore)
            {
                playerMain ??= GetComponent<PlayerMain>();
                playerMain?.PlayHit();
            }

            EventBus.Publish(new TakeDamageEvent { Amount = amount });
        }

        public class TakeDamageEvent
        {
            public int Amount;
        }
    }
}