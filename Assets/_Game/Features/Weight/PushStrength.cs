using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    /// <summary>
    /// Component that tracks the player's ability to push heavy objects.
    /// Higher push strength allows pushing heavier objects and pushing them faster.
    /// </summary>
    public class PushStrength : MonoBehaviour
    {
        [SerializeField] private float maxPushableWeight = 10f;
        [SerializeField] private float pushSpeedMultiplier = 1f;
        
        public float MaxPushableWeight
        {
            get => maxPushableWeight;
            set => maxPushableWeight = value;
        }
        
        public float PushSpeedMultiplier
        {
            get => pushSpeedMultiplier;
            set => pushSpeedMultiplier = value;
        }
        
        /// <summary>
        /// Check if the player can push an object with the given weight
        /// </summary>
        public bool CanPush(float objectWeight)
        {
            return objectWeight <= maxPushableWeight;
        }
        
        /// <summary>
        /// Get the effective push speed based on object weight and push strength
        /// Returns a multiplier between 0 and pushSpeedMultiplier
        /// </summary>
        public float GetPushSpeed(float objectWeight)
        {
            if (objectWeight > maxPushableWeight)
                return 0f;
            
            // Safety check for division by zero
            if (maxPushableWeight <= 0f)
                return pushSpeedMultiplier;
            
            // Heavier objects are pushed slower
            // At max weight, speed is reduced to 50% of pushSpeedMultiplier
            // At 0 weight, speed is 100% of pushSpeedMultiplier
            float weightRatio = Mathf.Clamp01(objectWeight / maxPushableWeight);
            float speedReduction = Mathf.Lerp(1f, 0.5f, weightRatio);
            
            return pushSpeedMultiplier * speedReduction;
        }
    }
}
