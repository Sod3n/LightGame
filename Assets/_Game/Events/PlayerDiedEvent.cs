using UnityEngine;

namespace Light_and_controller.Scripts.Events
{
    /// <summary>
    /// Event published when the player dies
    /// </summary>
    public class PlayerDiedEvent
    {
        public GameObject Player { get; }

        public PlayerDiedEvent(GameObject player)
        {
            Player = player;
        }
    }
}
