using Light_and_controller.Scripts.Components;
using UnityEngine;

namespace Light_and_controller.Scripts.AbilitiesSystem
{
    [CreateAssetMenu(fileName = "HealInLight", menuName = "Abilities/HealInLight")]
    public class HealInLightAbility : LevelAbility
    {
        [SerializeField] private HealOverTimeEffect.EffectData healData = new HealOverTimeEffect.EffectData
        {
            Amount = 1,
            Rate = 1f,
            IsInfinity = true,
            ResetTickOnHealthChange = true
        };
        
        public override void Activate(GameObject target)
        {
            // Check if component already exists
            var existingSystem = target.GetComponent<HealInLightSystem>();
            if (existingSystem != null)
            {
                existingSystem.enabled = true;
                existingSystem.Data = healData;
                Debug.Log("Heal in Light ability re-enabled!");
                return;
            }
            
            // Add LightDetector if not present
            if (target.GetComponent<LightDetector>() == null)
            {
                target.AddComponent<LightDetector>();
            }
            
            // Add the heal system
            var healSystem = target.AddComponent<HealInLightSystem>();
            healSystem.Data = healData;
            
            Debug.Log("Heal in Light ability activated!");
        }
        
        public override void Deactivate(GameObject target)
        {
            var healSystem = target.GetComponent<HealInLightSystem>();
            if (healSystem != null)
            {
                healSystem.enabled = false;
                Debug.Log("Heal in Light ability deactivated");
            }
        }
    }
}
