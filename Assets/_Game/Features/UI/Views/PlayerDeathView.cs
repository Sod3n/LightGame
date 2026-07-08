using System.Collections;
using DG.Tweening;
using Light_and_controller.Scripts;
using Light_and_controller.Scripts.Events;
using Light_and_controller.Scripts.Systems;
using Light_and_controller.Scripts.Components;
using UnityEngine;

namespace Light_and_controller.Scripts.UI.Views
{
    public class PlayerDeathView : MonoBehaviour
    {
        [Header("Death Fade Settings")]
        [SerializeField] private float deathFadeDuration = 0.5f;
        [SerializeField] private Ease deathFadeEase = Ease.InCubic;
        
        [Header("Respawn Delay")]
        [SerializeField] private float delayAfterFade = 0.1f;

        private bool isHandlingDeath = false;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private IEnumerator HandleDeathCoroutine(GameObject player)
        {
            isHandlingDeath = true;

            // Fade all lights to black quickly
            GlobalLightEvents.FadeToBlack(deathFadeDuration, deathFadeEase);

            // Wait for fade to complete
            yield return new WaitForSeconds(deathFadeDuration + delayAfterFade);

            // Respawn player at checkpoint instead of reloading level
            RespawnAtCheckpoint(player);

            // Wait a frame for position to be updated
            yield return null;

            // Fade lights back in
            GlobalLightEvents.FadeToFull(deathFadeDuration, deathFadeEase);

            isHandlingDeath = false;
        }

        private void RespawnAtCheckpoint(GameObject player)
        {
            if (player == null)
            {
                Debug.LogError("Player reference is null! Cannot respawn at checkpoint.");
                return;
            }

            // If there's an active checkpoint, respawn there
            if (Checkpoint.Active != null)
            {
                Checkpoint.Active.RespawnPlayer(player);
                Debug.Log($"Player respawned at checkpoint: {Checkpoint.Active.RespawnPosition}");
            }
            else
            {
                // Fallback: reload level if no checkpoint is active
                Debug.LogWarning("No active checkpoint found! Falling back to level reload.");
                var currentLevel = SceneLoader.GetCurrentLevel();
                if (currentLevel.HasValue)
                {
                    SceneLoader.LoadLevel(currentLevel.Value);
                }
            }
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            if (!isHandlingDeath)
            {
                StartCoroutine(HandleDeathCoroutine(evt.Player));
            }
        }
    }
}
