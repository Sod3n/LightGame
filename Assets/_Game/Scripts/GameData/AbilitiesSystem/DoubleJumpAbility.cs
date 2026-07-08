using UnityEngine;

namespace Light_and_controller.Scripts.AbilitiesSystem
{
    [CreateAssetMenu(fileName = "ExtraJump", menuName = "Abilities/ExtraJump")]
    public class DoubleJumpAbility : LevelAbility
    {
        [SerializeField] private PlayerData.JumpVariables.JumpInfo jumpInfo;
        
        public override void Activate(GameObject target)
        {
            if (target.TryGetComponent<PlayerMain>(out var player))
            {
                var jumpData = player.PlayerData.Jump;
                
                // Add the jump if configured
                if (jumpInfo != null)
                {
                    jumpData.Jumps.Add(jumpInfo);
                    Debug.Log($"Extra jump added! Total jumps: {jumpData.Jumps.Count}");
                }
                else
                {
                    Debug.LogWarning("ExtraJump ability has no jump info configured!");
                }
            }
        }
        
        public override void Deactivate(GameObject target)
        {
            if (target.TryGetComponent<PlayerMain>(out var player))
            {
                var jumpData = player.PlayerData.Jump;
                
                // Remove the last jump added (if there's more than the base jump)
                if (jumpData.Jumps.Count > 1)
                {
                    jumpData.Jumps.RemoveAt(jumpData.Jumps.Count - 1);
                    Debug.Log($"Extra jump removed! Total jumps: {jumpData.Jumps.Count}");
                }
            }
        }
    }
}