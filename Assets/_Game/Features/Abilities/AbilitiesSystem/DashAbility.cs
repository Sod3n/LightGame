using UnityEngine;

namespace Light_and_controller.Scripts.AbilitiesSystem
{
    [CreateAssetMenu(fileName = "Dash", menuName = "Abilities/Dash")]
    public class DashAbility : LevelAbility
    {
        public override void Activate(GameObject target)
        {
            if (target.TryGetComponent<PlayerMain>(out var player))
            {
                player.PlayerData.Dash.IsDashEnabled = true;
                Debug.Log("Dash ability activated!");
            }
        }
        
        public override void Deactivate(GameObject target)
        {
            if (target.TryGetComponent<PlayerMain>(out var player))
            {
                player.PlayerData.Dash.IsDashEnabled = false;
                Debug.Log("Dash ability deactivated");
            }
        }
    }
}
