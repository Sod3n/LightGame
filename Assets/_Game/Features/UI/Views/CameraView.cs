using System;
using System.Collections;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Events;
using Light_and_controller.Scripts.Systems;
using R3;
using Unity.Cinemachine;
using UnityEngine;

namespace Light_and_controller.Scripts.UI
{
    public class CameraView : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private float defaultShakeIntensity = 5f;
        [SerializeField] private float defaultShakeDuration = 0.3f;

        private CinemachineBasicMultiChannelPerlin noise;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            // Get the noise component from the camera
            if (cinemachineCamera != null)
            {
                // Try to get existing noise component from the camera's GameObject
                noise = cinemachineCamera.gameObject.GetComponent<CinemachineBasicMultiChannelPerlin>();
                
                // Initialize noise to 0 if found
                if (noise != null)
                {
                    noise.AmplitudeGain = 0f;
                    noise.FrequencyGain = 0f;
                }
                else
                {
                    Debug.LogWarning($"CinemachineBasicMultiChannelPerlin component not found on {cinemachineCamera.name}. Please add it manually in the Inspector under Noise settings.");
                }
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerHealthSystem.TakeDamageEvent>(OnTakeDamage);
            EventBus.Subscribe<ShakeCameraEvent>(OnShakeCamera);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerHealthSystem.TakeDamageEvent>(OnTakeDamage);
            EventBus.Unsubscribe<ShakeCameraEvent>(OnShakeCamera);

            // Stop any ongoing shake
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }
        }
        
        private void OnTakeDamage(PlayerHealthSystem.TakeDamageEvent _)
        {
            ShakeCamera(defaultShakeIntensity, defaultShakeDuration);
        }

        private void OnShakeCamera(ShakeCameraEvent shakeCameraEvent)
        {
            float intensity = shakeCameraEvent.Intensity > 0 ? shakeCameraEvent.Intensity : defaultShakeIntensity;
            float duration = shakeCameraEvent.Duration > 0 ? shakeCameraEvent.Duration : defaultShakeDuration;

            ShakeCamera(intensity, duration);
        }

        public void ShakeCamera(float intensity, float duration)
        {
            if (noise == null) return;

            // Stop any existing shake
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
        }   

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            // Set the shake intensity
            noise.AmplitudeGain = intensity;
            noise.FrequencyGain = intensity;

            // Wait for the duration
            yield return new WaitForSeconds(duration);

            // Smoothly reduce the shake
            float elapsed = 0f;
            float fadeOutDuration = 0.1f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                noise.AmplitudeGain = Mathf.Lerp(intensity, 0f, t);
                noise.FrequencyGain = Mathf.Lerp(intensity, 0f, t);
                yield return null;
            }

            // Ensure it's completely stopped
            noise.AmplitudeGain = 0f;
            noise.FrequencyGain = 0f;

            shakeCoroutine = null;
        }
    }
}