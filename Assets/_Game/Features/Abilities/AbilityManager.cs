using System.Collections.Generic;
using Light_and_controller.Scripts.AbilitiesSystem;
using UnityEngine;

namespace Light_and_controller.Scripts.Systems
{
    public class AbilityManager : MonoBehaviour
    {
        private HashSet<LevelAbility> _activeAbilities = new();
        private HashSet<LevelAbility> _permanentAbilities = new();
        private HashSet<LevelAbility> _disabledAbilities = new();
        private GameObject _player;
        private bool _abilitiesDisabled = false;
        
        private void Start()
        {
            GD.Init();
            _player = GameObject.FindGameObjectWithTag("Player");
            
            // Activate all AlwaysUnlocked abilities at start
            ActivateAlwaysUnlockedAbilities();
            
            // Unlock abilities for current level
            var currentLevel = SceneLoader.GetCurrentLevel();
            if (currentLevel.HasValue)
            {
                UnlockAbilitiesUpToLevel(currentLevel.Value);
            }
        }
        
        private void ActivateAlwaysUnlockedAbilities()
        {
            if (GD.LevelAbilities == null) return;
            
            foreach (var ability in GD.LevelAbilities.Abilities)
            {
                if (ability.unlockBehavior == UnlockBehavior.AlwaysUnlocked)
                {
                    UnlockAbility(ability);
                }
            }
        }
        
        public void UnlockAbilitiesForLevel(SceneName sceneName)
        {
            var newAbilities = GD.LevelAbilities.GetAbilitiesForLevel(sceneName);
            foreach (var ability in newAbilities)
            {
                UnlockAbility(ability);
            }
        }
        
        /// <summary>
        /// Unlock all abilities up to and including the specified level.
        /// Useful for save/load systems or starting mid-game.
        /// </summary>
        public void UnlockAbilitiesUpToLevel(SceneName targetLevel)
        {
            if (GD.LevelAbilities == null) return;
            
            var abilitiesToUnlock = GD.LevelAbilities.GetAbilitiesUpToLevel(targetLevel);
            foreach (var ability in abilitiesToUnlock)
            {
                UnlockAbility(ability);
            }
            
            Debug.Log($"Unlocked {abilitiesToUnlock.Count} abilities up to level {targetLevel}");
        }
        
        public void UnlockAbility(LevelAbility ability)
        {
            if (!_activeAbilities.Contains(ability))
            {
                _activeAbilities.Add(ability);
                ability.Activate(_player);
                
                // Mark as permanent if it's UnlockAtLevel or AlwaysUnlocked
                if (ability.unlockBehavior == UnlockBehavior.UnlockAtLevel || 
                    ability.unlockBehavior == UnlockBehavior.AlwaysUnlocked)
                {
                    _permanentAbilities.Add(ability);
                }
            }
        }
        
        public bool HasAbility(LevelAbility ability)
        {
            return _activeAbilities.Contains(ability);
        }
        
        public void RemoveAbility(LevelAbility ability)
        {
            // Don't allow removal of permanent abilities
            if (_permanentAbilities.Contains(ability))
            {
                Debug.LogWarning($"Cannot remove permanent ability: {ability.name}");
                return;
            }
            
            if (_activeAbilities.Remove(ability))
            {
                ability.Deactivate(_player);
            }
        }

        public void DisableAllAbilities()
        {
            if (_abilitiesDisabled) return;

            _abilitiesDisabled = true;
            _disabledAbilities.Clear();

            foreach (var ability in _activeAbilities)
            {
                // Only disable abilities that have disableInNearDeath set to true
                if (ability.disableInNearDeath)
                {
                    ability.Deactivate(_player);
                    _disabledAbilities.Add(ability);
                }
            }

            Debug.Log($"Disabled {_disabledAbilities.Count} abilities in near death state");
        }

        public void EnableAllAbilities()
        {
            if (!_abilitiesDisabled) return;

            _abilitiesDisabled = false;

            foreach (var ability in _disabledAbilities)
            {
                if (_activeAbilities.Contains(ability))
                {
                    ability.Activate(_player);
                }
            }

            Debug.Log($"Re-enabled {_disabledAbilities.Count} abilities");
            _disabledAbilities.Clear();
        }
    }
}