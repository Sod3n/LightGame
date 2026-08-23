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
                ambientLight.intensity = 0f;

            if (voidMaterial != null)
                voidMaterial.SetFloat(VoidStrengthId, 1f);
        }
    }
}
