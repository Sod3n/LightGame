using System.Collections.Generic;
using Light_and_controller.Scripts;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace Light_and_controller.Scripts.AbilitiesSystem
{
    [CreateAssetMenu(fileName = "LevelAbilities", menuName = "Abilities/LevelAbilities")]
    public class LevelAbilities : ScriptableObject
    {
        [SerializeField] private List<LevelAbility> abilities = new();
        [SerializeField] private bool loadFromAddressables = false;
        [SerializeField] private string addressableLabel = "Abilities";
        
        private bool _isLoaded = false;
        
        public List<LevelAbility> Abilities => abilities;
        
        /// <summary>
        /// Load all abilities from Addressables with the specified label
        /// </summary>
        public void LoadAbilitiesFromAddressables()
        {
            if (_isLoaded) return;
            
            if (!loadFromAddressables)
            {
                _isLoaded = true;
                return;
            }
            
            var loadedAbilities = Addressables.LoadAssetsAsync<LevelAbility>(addressableLabel).WaitForCompletion();
            
            abilities.Clear();
            abilities.AddRange(loadedAbilities);
            
            _isLoaded = true;
            Debug.Log($"Loaded {abilities.Count} abilities from Addressables with label '{addressableLabel}'");
        }
        
        // Helper methods
        public List<LevelAbility> GetAbilitiesForLevel(SceneName sceneName)
        {
            var abilities = new List<LevelAbility>();
            foreach (var ability in this.abilities)
            {
                if (ability.unlockAtLevel == sceneName)
                    abilities.Add(ability);
            }
            return abilities;
        }
        
        public List<LevelAbility> GetAbilitiesUpToLevel(SceneName targetLevel)
        {
            var result = new List<LevelAbility>();
            
            // Get level order from GD
            if (GD.LevelOrder == null || GD.LevelOrder.Value == null)
            {
                Debug.LogWarning("LevelOrder not initialized or empty");
                return result;
            }
            
            // Find the index of target level
            int targetIndex = GD.LevelOrder.Value.IndexOf(targetLevel);
            if (targetIndex == -1)
            {
                // If level not in order (test level), return all abilities for testing
                Debug.Log($"Level {targetLevel} not found in LevelOrder - returning all abilities for testing");
                return new List<LevelAbility>(abilities);
            }
            
            // Collect all abilities up to and including target level
            foreach (var ability in abilities)
            {
                int abilityLevelIndex = GD.LevelOrder.Value.IndexOf(ability.unlockAtLevel);
                if (abilityLevelIndex != -1 && abilityLevelIndex <= targetIndex)
                {
                    result.Add(ability);
                }
            }
            
            return result;
        }
    }
}