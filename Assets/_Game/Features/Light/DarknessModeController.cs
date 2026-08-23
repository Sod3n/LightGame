using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LightGame.Features
{
    [ExecuteAlways]
    public class DarknessModeController : MonoBehaviour
    {
        [Header("Ambient")]
        [SerializeField] private Light2D ambientLight;

        [Header("Void Overlay")]
        [SerializeField] private Material voidMaterial;

        [Header("Darkness Tuning")]
        [Tooltip("Base light on all geometry so silhouettes stay readable in the dark. 0 = pure black.")]
        [SerializeField, Range(0f, 1f)] private float ambientIntensity = 0.15f;
        [Tooltip("How opaque the void overlay is. 1 = fully black; lower lets the dim, ambient-lit scene show through.")]
        [SerializeField, Range(0f, 1f)] private float voidStrength = 0.6f;

        private static readonly int VoidStrengthId = Shader.PropertyToID("_VoidStrength");

        private void Awake()
        {
            if (ambientLight == null)
                ambientLight = FindAmbientGlobalLight();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private static Light2D FindAmbientGlobalLight()
        {
            foreach (var light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (light.lightType == Light2D.LightType.Global)
                    return light;
            }

            return null;
        }

        private void Apply()
        {
            if (ambientLight != null)
                ambientLight.intensity = ambientIntensity;

            if (voidMaterial != null)
                voidMaterial.SetFloat(VoidStrengthId, voidStrength);
        }
    }
}
