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

        [Header("Fog Overlay")]
        [SerializeField] private Material fogMaterial;
        [SerializeField] private float voidFogStrength = 0.85f;

        private static readonly int VoidStrengthId = Shader.PropertyToID("_VoidStrength");
        private static readonly int FogStrengthId = Shader.PropertyToID("_FogStrength");

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
                ambientLight.intensity = 0f;

            if (voidMaterial != null)
                voidMaterial.SetFloat(VoidStrengthId, 1f);

            if (fogMaterial != null)
                fogMaterial.SetFloat(FogStrengthId, voidFogStrength);
        }
    }
}
