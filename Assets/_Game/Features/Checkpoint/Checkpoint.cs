using System;
using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    public class Checkpoint : MonoBehaviour
    {
        public static Checkpoint Active;

        [Header("Checkpoint Settings")]
        [SerializeField] private bool setAsStartCheckpoint = false;
        [SerializeField] private Vector2 respawnOffset = Vector2.zero;

        public Vector2 RespawnPosition => (Vector2)transform.position + respawnOffset;

        private void Awake()
        {
            if (setAsStartCheckpoint)
            {
                Active = this;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                SetAsActiveCheckpoint();
            }
        }

        public void SetAsActiveCheckpoint()
        {
            Active = this;
            Debug.Log($"Checkpoint activated at: {transform.position}");

            // You can add visual feedback here (particle effects, sound, etc.)
        }

        public void RespawnPlayer(GameObject player)
        {
            if (player != null)
            {
                player.transform.position = RespawnPosition;

                // Reset player rigidbody velocity if it exists
                var rigidbody = player.GetComponent<Rigidbody2D>();
                if (rigidbody != null)
                {
                    rigidbody.linearVelocity = Vector2.zero;
                    rigidbody.angularVelocity = 0f;
                }

                // Reset player health if needed
                var healthSystem = player.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.Heal(int.MaxValue);
                }
            }
        }
    }
}                     