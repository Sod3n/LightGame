using System.Collections.Generic;
using LightGame;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

using LightGame.Globals;
namespace LightGame.Features.Abilities
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
        public List<LevelAbility> GetAbilitiesForLevel(string sceneName)
        {
            var abilities = new List<LevelAbility>();
            foreach (var ability in this.abilities)
            {
                if (ability.unlockAtLevel?.SceneName == sceneName)
                    abilities.Add(ability);
            }
            return abilities;
        }

        public List<LevelAbility> GetAbilitiesUpToLevel(string targetLevel)
        {
            var result = new List<LevelAbility>();

            // Get level order from GD
            if (Game.LevelOrder == null || Game.LevelOrder.Levels == null)
            {
                Debug.LogWarning("LevelOrder not initialized or empty");
                return result;
            }

            // Find the index of target level
            int targetIndex = Game.LevelOrder.IndexOf(targetLevel);
            if (targetIndex == -1)
            {
                // If level not in order (test level), return all abilities for testing
                Debug.Log($"Level {targetLevel} not found in LevelOrder - returning all abilities for testing");
                return new List<LevelAbility>(abilities);
            }

            // Collect all abilities up to and including target level
            foreach (var ability in abilities)
            {
                int abilityLevelIndex = Game.LevelOrder.IndexOf(ability.unlockAtLevel?.SceneName);
                if (abilityLevelIndex != -1 && abilityLevelIndex <= targetIndex)
                {
                    result.Add(ability);
                }
            }

            return result;
        }
    }
}