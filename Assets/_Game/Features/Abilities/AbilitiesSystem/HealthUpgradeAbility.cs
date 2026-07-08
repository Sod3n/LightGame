using UnityEngine;

namespace Light_and_controller.Scripts.AbilitiesSystem
{
    [CreateAssetMenu(fileName = "HealthUpgrade", menuName = "Abilities/HealthUpgrade")]
    public class HealthUpgradeAbility : LevelAbility
    {
        [SerializeField] private int healthIncrease = 1;
        [SerializeField] private bool healOnActivate = true;
        
        private int _previousMaxHealth;
        
        public override void Activate(GameObject target)
        {
            var healthSystem = target.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                _previousMaxHealth = healthSystem.MaxHealth;
                healthSystem.MaxHealth += healthIncrease;
                
                // Optionally heal the player by the amount increased
                if (healOnActivate)
                {
                    healthSystem.Heal(healthIncrease);
                }
                
                Debug.Log($"Health upgraded! Max health: {healthSystem.MaxHealth} (+{healthIncrease})");
            }
        }
        
        public override void Deactivate(GameObject target)
        {
            var healthSystem = target.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.MaxHealth = _previousMaxHealth;
                
                // Clamp current health to new max if it exceeds
                healthSystem.ClampHealthToMax();
                
                Debug.Log($"Health upgrade deactivated. Max health: {healthSystem.MaxHealth}");
            }
        }
    }
}
