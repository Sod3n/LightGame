using System.Collections;
using DG.Tweening;
using Light_and_controller.Scripts;
using Light_and_controller.Scripts.Events;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

namespace Light_and_controller.Scripts.UI.Views
{
    public class LevelChangeView : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private float fadeOutDuration = 1f;
        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private Ease fadeEase = Ease.InOutQuad;
        
        [Header("Scene Load Delay")]
        [SerializeField] private float delayBeforeLoad = 0.2f;
        [SerializeField] private float delayAfterLoad = 0.2f;

        private bool isTransitioning = false;

        private void OnEnable()
        {
            EventBus.Subscribe<RequestLevelChangeEvent>(OnRequestLevelChange);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RequestLevelChangeEvent>(OnRequestLevelChange);
        }

        private void OnRequestLevelChange(RequestLevelChangeEvent evt)
        {
            if (!isTransitioning)
            {
                ChangeLevel(evt.TargetScene);
            }
        }

        /// <summary>
        /// Change to a new level with light fade transition
        /// </summary>
        public void ChangeLevel(SceneName targetScene)
        {
            if (!isTransitioning)
            {
                StartCoroutine(ChangeLevelCoroutine(targetScene));
            }
        }

        private IEnumerator ChangeLevelCoroutine(SceneName targetScene)
        {
            isTransitioning = true;

            // Store the current light intensity before fading
            Light2DGlobalListener.StoreCurrentIntensity(0f); // Will fade to black, so store 0

            // First, fade Default lights to black
            GlobalLightEvents.FadeToBlack(fadeOutDuration, fadeEase, LightType.Default);

            // Wait for Default lights fade out to complete
            yield return new WaitForSeconds(fadeOutDuration);

            // Then, fade LevelChange lights to black
            GlobalLightEvents.FadeToBlack(fadeOutDuration, fadeEase, LightType.LevelChange);

            // Wait for LevelChange lights fade out to complete
            yield return new WaitForSeconds(fadeOutDuration + delayBeforeLoad);

            // Load the new level (unloads previous, ensures Shared is loaded)
            SceneLoader.LoadLevel(targetScene);

            // Wait a bit for scene to initialize
            yield return new WaitForSeconds(delayAfterLoad);

            // Fade LevelChange lights back in first
            GlobalLightEvents.FadeToFull(fadeInDuration, fadeEase, LightType.LevelChange);

            // Wait for LevelChange lights to fade in
            yield return new WaitForSeconds(fadeInDuration);

            // Then fade Default lights back in
            GlobalLightEvents.FadeToFull(fadeInDuration, fadeEase, LightType.Default);

            // Wait for Default lights fade in to complete
            yield return new WaitForSeconds(fadeInDuration);

            // Clear stored intensity
            Light2DGlobalListener.ClearStoredIntensity();

            isTransitioning = false;
        }

        /// <summary>
        /// Change level immediately without fade
        /// </summary>
        public void ChangeLevelImmediate(SceneName targetScene)
        {
            if (!isTransitioning)
            {
                SceneLoader.LoadLevel(targetScene);
            }
        }

        /// <summary>
        /// Check if currently transitioning
        /// </summary>
        public bool IsTransitioning => isTransitioning;
    }
}
