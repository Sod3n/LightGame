using UnityEngine;

namespace Light_and_controller.Scripts.AbilitiesSystem
{
    [CreateAssetMenu(fileName = "Teleport", menuName = "Abilities/Teleport")]
    public class TeleportAbility : LevelAbility
    {
        public override void Activate(GameObject target)
        {
            var teleportSystem = target.GetComponent<PlayerTeleportSystem>();
            if (teleportSystem == null)
            {
                teleportSystem = target.AddComponent<PlayerTeleportSystem>();
                Debug.Log("Teleport ability activated!");
            }
            else
            {
                teleportSystem.enabled = true;
                Debug.Log("Teleport ability re-enabled!");
            }
        }
        
        public override void Deactivate(GameObject target)
        {
            var teleportSystem = target.GetComponent<PlayerTeleportSystem>();
            if (teleportSystem != null)
            {
                teleportSystem.enabled = false;
                Debug.Log("Teleport ability deactivated");
            }
        }
    }
}
