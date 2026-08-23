using System;
using UnityEngine;

using LightGame.Globals;
namespace LightGame.Features.Abilities
{
    public enum UnlockBehavior
    {
        UnlockAtLevel,      // Unlocks at specific level and stays unlocked
        AlwaysUnlocked,     // Always active, unlocked from start
        TemporaryUnlock     // Unlocks at level but can be locked again
    }
    
    public class LevelAbility : ScriptableObject
    {
        public SceneReference unlockAtLevel;
        public UnlockBehavior unlockBehavior = UnlockBehavior.UnlockAtLevel;
        public bool disableInNearDeath = true;

        public virtual void Activate(GameObject target) {}
        public virtual void Deactivate(GameObject target) {}
    }
}