using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core._.UI
{
    public class GlobalLight2DController : TweenableBase
    {
        public static GlobalLight2DController Instance { get; private set; }
        
        [Header("Light Settings")]
        [SerializeField] private float targetIntensity = 1f;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.InOutQuad;
        
        [Header("Light Collection")]
        [SerializeField] private bool findLightsOnStart = true;
        [SerializeField] private List<Light2D> lights = new List<Light2D>();
        
        private List<float> originalIntensities = new List<float>();
        
        public override Tween Tween { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple GlobalLight2DController instances found. Destroying duplicate on {gameObject.name}");
                Destroy(this);
                return;
            }
            
            Instance = this;
        }

        private void Start()
        {
            if (findLightsOnStart)
            {
                FindAllLights();
            }
            
            StoreOriginalIntensities();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Finds all Light2D components in the scene
        /// </summary>
        public void FindAllLights()
        {
            lights.Clear();
            lights.AddRange(FindObjectsByType<Light2D>(FindObjectsSortMode.None));
            StoreOriginalIntensities();
        }

        /// <summary>
        /// Stores the original intensities of all lights
        /// </summary>
        private void StoreOriginalIntensities()
        {
            originalIntensities.Clear();
            foreach (var light in lights)
            {
                if (light != null)
                {
                    originalIntensities.Add(light.intensity);
                }
            }
        }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] != null)
                {
                    var light = lights[i];
                    var startIntensity = light.intensity;
                    sequence.Join(DOVirtual.Float(startIntensity, targetIntensity, duration, 
                        t => light.intensity = t).SetEase(ease));
                }
            }
            
            return sequence;
        }

        /// <summary>
        /// Set intensity for all lights with tweening
        /// </summary>
        public void SetIntensity(float intensity)
        {
            targetIntensity = intensity;
            Play();
        }

        /// <summary>
        /// Set intensity for all lights with custom duration
        /// </summary>
        public void SetIntensity(float intensity, float customDuration)
        {
            targetIntensity = intensity;
            duration = customDuration;
            Play();
        }

        /// <summary>
        /// Set intensity immediately without tweening
        /// </summary>
        public void SetIntensityImmediate(float intensity)
        {
            foreach (var light in lights)
            {
                if (light != null)
                {
                    light.intensity = intensity;
                }
            }
        }

        /// <summary>
        /// Restore all lights to their original intensities
        /// </summary>
        public void RestoreOriginalIntensities()
        {
            for (int i = 0; i < lights.Count && i < originalIntensities.Count; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].intensity = originalIntensities[i];
                }
            }
        }

        /// <summary>
        /// Restore all lights to their original intensities with tweening
        /// </summary>
        public void RestoreOriginalIntensitiesWithTween()
        {
            Tween?.Complete();
            var sequence = DOTween.Sequence();
            
            for (int i = 0; i < lights.Count && i < originalIntensities.Count; i++)
            {
                if (lights[i] != null)
                {
                    var light = lights[i];
                    var originalIntensity = originalIntensities[i];
                    sequence.Join(DOVirtual.Float(light.intensity, originalIntensity, duration, 
                        t => light.intensity = t).SetEase(ease));
                }
            }
            
            Tween = sequence;
        }

        /// <summary>
        /// Fade all lights to black (intensity 0)
        /// </summary>
        public void FadeToBlack()
        {
            SetIntensity(0f);
        }

        /// <summary>
        /// Fade all lights to full brightness
        /// </summary>
        public void FadeToFull()
        {
            SetIntensity(1f);
        }

        /// <summary>
        /// Add a light to the controlled list
        /// </summary>
        public void AddLight(Light2D light)
        {
            if (light != null && !lights.Contains(light))
            {
                lights.Add(light);
                originalIntensities.Add(light.intensity);
            }
        }

        /// <summary>
        /// Remove a light from the controlled list
        /// </summary>
        public void RemoveLight(Light2D light)
        {
            int index = lights.IndexOf(light);
            if (index >= 0)
            {
                lights.RemoveAt(index);
                if (index < originalIntensities.Count)
                {
                    originalIntensities.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Static method to register a light to the global controller
        /// </summary>
        public static void RegisterLight(Light2D light)
        {
            if (Instance != null)
            {
                Instance.AddLight(light);
            }
            else
            {
                Debug.LogWarning($"GlobalLight2DController.RegisterLight: No instance found in scene for light on {light.gameObject.name}");
            }
        }

        /// <summary>
        /// Static method to unregister a light from the global controller
        /// </summary>
        public static void UnregisterLight(Light2D light)
        {
            if (Instance != null)
            {
                Instance.RemoveLight(light);
            }
        }

        /// <summary>
        /// Clear all lights from the controlled list
        /// </summary>
        public void ClearLights()
        {
            lights.Clear();
            originalIntensities.Clear();
        }

        private void OnValidate()
        {
            targetIntensity = Mathf.Max(0f, targetIntensity);
            duration = Mathf.Max(0f, duration);
        }
    }
}
