using Light_and_controller.Scripts.Components;
using UnityEngine;

namespace Light_and_controller.Scripts.AbilitiesSystem
{
    [CreateAssetMenu(fileName = "DamageInDark", menuName = "Abilities/DamageInDark")]
    public class DamageInDarkAbility : LevelAbility
    {
        [SerializeField] private DamageOverTimeEffect.EffectData damageData = new DamageOverTimeEffect.EffectData
        {
            Amount = 1,
            Rate = 1f,
            IsInfinity = true,
            ResetTickOnHealthChange = true
        };
        
        public override void Activate(GameObject target)
        {
            // Check if component already exists
            var existingSystem = target.GetComponent<DamageInDarkSystem>();
            if (existingSystem != null)
            {
                existingSystem.enabled = true;
                existingSystem.Data = damageData;
                Debug.Log("Damage in Dark ability re-enabled!");
                return;
            }
            
            // Add LightDetector if not present
            if (target.GetComponent<LightDetector>() == null)
            {
                target.AddComponent<LightDetector>();
            }
            
            // Add the damage system
            var damageSystem = target.AddComponent<DamageInDarkSystem>();
            damageSystem.Data = damageData;
            
            Debug.Log("Damage in Dark ability activated!");
        }
        
        public override void Deactivate(GameObject target)
        {
            var damageSystem = target.GetComponent<DamageInDarkSystem>();
            if (damageSystem != null)
            {
                damageSystem.enabled = false;
                Debug.Log("Damage in Dark ability deactivated");
            }
        }
    }
}
