using Light_and_controller.Scripts.Components;
using UnityEngine;

namespace Light_and_controller.Scripts.AbilitiesSystem
{
    [CreateAssetMenu(fileName = "PushStrength", menuName = "Abilities/PushStrength")]
    public class PushStrengthAbility : LevelAbility
    {
        [SerializeField] private float weightIncrease = 5f;
        [SerializeField] private float speedMultiplierIncrease = 0.2f;
        
        private float _previousMaxWeight;
        private float _previousSpeedMultiplier;
        
        public override void Activate(GameObject target)
        {
            var pushStrength = target.GetComponent<PushStrength>();
            
            // Add component if it doesn't exist
            if (pushStrength == null)
            {
                pushStrength = target.AddComponent<PushStrength>();
                _previousMaxWeight = pushStrength.MaxPushableWeight;
                _previousSpeedMultiplier = pushStrength.PushSpeedMultiplier;
            }
            else
            {
                _previousMaxWeight = pushStrength.MaxPushableWeight;
                _previousSpeedMultiplier = pushStrength.PushSpeedMultiplier;
            }
            
            // Increase push capabilities
            pushStrength.MaxPushableWeight += weightIncrease;
            pushStrength.PushSpeedMultiplier += speedMultiplierIncrease;
            
            Debug.Log($"Push strength upgraded! Max weight: {pushStrength.MaxPushableWeight} (+{weightIncrease}), Speed: {pushStrength.PushSpeedMultiplier:F2}x (+{speedMultiplierIncrease:F2}x)");
        }
        
        public override void Deactivate(GameObject target)
        {
            var pushStrength = target.GetComponent<PushStrength>();
            if (pushStrength != null)
            {
                pushStrength.MaxPushableWeight = _previousMaxWeight;
                pushStrength.PushSpeedMultiplier = _previousSpeedMultiplier;
                
                Debug.Log($"Push strength downgraded. Max weight: {pushStrength.MaxPushableWeight}, Speed: {pushStrength.PushSpeedMultiplier:F2}x");
            }
        }
    }
}
