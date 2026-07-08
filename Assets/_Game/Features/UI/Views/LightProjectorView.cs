using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Light_and_controller.Scripts.UI.Views
{
    public class LightProjectorView : Togglable
    {
        [SerializeField] private List<GameObject> lights;
        [SerializeField] private float fadeOutDuration = 0.4f;
        [SerializeField] private float fadeInDuration = 0.4f;
        
        [Header("Sprite Swap")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;
        [SerializeField] private Sprite enabledSprite;
        [SerializeField] private Sprite disabledSprite;

        private int pendingFadeOuts = 0;

        public override void Enable()
        {
            SwapSprite(enabledSprite);
            
            foreach (var lightObj in lights)
            {
                lightObj.SetActive(true);

                // Fade in lights that have the listener component
                var listener = lightObj.GetComponent<Light2DGlobalListener>();
                if (listener != null)
                {
                    listener.FadeIn(fadeInDuration);
                }
            }
        }

        public override void Disable()
        {
            // Count how many lights have listeners
            pendingFadeOuts = 0;
            foreach (var lightObj in lights)
            {
                var listener = lightObj.GetComponent<Light2DGlobalListener>();
                if (listener != null)
                {
                    pendingFadeOuts++;
                }
            }

            // If no lights have listeners, disable immediately
            if (pendingFadeOuts == 0)
            {
                DisableAllLights();
                return;
            }

            // Fade out lights with listeners
            foreach (var lightObj in lights)
            {
                var listener = lightObj.GetComponent<Light2DGlobalListener>();
                if (listener != null)
                {
                    listener.FadeOut(fadeOutDuration, OnFadeOutComplete);
                }
            }
        }

        private void OnFadeOutComplete()
        {
            pendingFadeOuts--;

            // When all fades are complete, disable the lights
            if (pendingFadeOuts <= 0)
            {
                DisableAllLights();
            }
        }

        private void DisableAllLights()
        {
            SwapSprite(disabledSprite);
            
            foreach (var lightObj in lights)
            {
                lightObj.SetActive(false);
            }
        }
        
        private void SwapSprite(Sprite sprite)
        {
            if (targetSpriteRenderer != null && sprite != null)
            {
                targetSpriteRenderer.sprite = sprite;
            }
        }
    }
}