using Core._.UI;
using LightGame.Audio;
using UnityEngine;

namespace Light_and_controller.Scripts.UI
{
    /// <summary>
    /// View component for JumpPad that handles visual feedback through tweens.
    /// Subscribes to JumpPad Action events and triggers appropriate animations.
    /// </summary>
    public class JumpPadView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private JumpPad jumpPad;
        [SerializeField] private JumpPadCompressTween compressTween;
        [SerializeField] private JumpPadBounceTween bounceTween;
        [SerializeField] private JumpPadReadyTween readyTween;
        
        [Header("Optional Visual Effects")]
        [SerializeField] private ParticleSystem landParticles;
        [SerializeField] private ParticleSystem bounceParticles;
        [SerializeField] private ParticleSystem readyParticles;
        
        [Header("Optional Audio")]
        [SerializeField] private SoundData soundData;
        
        [Header("Optional Sprite Effects")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color activeColor = Color.yellow;
        [SerializeField] private Color disabledColor = Color.gray;
        [SerializeField] private float colorTransitionDuration = 0.2f;
        
        private void Awake()
        {
            if (jumpPad == null)
                jumpPad = GetComponent<JumpPad>();
            
            if (compressTween == null)
                compressTween = GetComponent<JumpPadCompressTween>();
            
            if (bounceTween == null)
                bounceTween = GetComponent<JumpPadBounceTween>();
            
            if (readyTween == null)
                readyTween = GetComponent<JumpPadReadyTween>();
        }
        
        private void Start()
        {
            // Initialize sprite color based on active state
            if (spriteRenderer != null && jumpPad != null)
            {
                spriteRenderer.color = jumpPad.IsActive ? normalColor : disabledColor;
            }
        }
        
        private void OnEnable()
        {
            if (jumpPad != null)
            {
                jumpPad.OnPlayerLand += OnPlayerLand;
                jumpPad.OnPlayerBounce += OnPlayerBounce;
                jumpPad.OnCooldownComplete += OnCooldownComplete;
            }
        }
        
        private void OnDisable()
        {
            if (jumpPad != null)
            {
                jumpPad.OnPlayerLand -= OnPlayerLand;
                jumpPad.OnPlayerBounce -= OnPlayerBounce;
                jumpPad.OnCooldownComplete -= OnCooldownComplete;
            }
        }
        
        private void OnPlayerLand(PlayerMain player)
        {
            // Play compress animation
            if (compressTween != null)
            {
                compressTween.Play();
            }
            
            // Play land particles
            if (landParticles != null)
            {
                landParticles.Play();
            }
            
            // Change sprite color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = activeColor;
            }
        }
        
        private void OnPlayerBounce(PlayerMain player)
        {
            // Play bounce animation
            if (bounceTween != null)
            {
                bounceTween.Play();
            }
            
            // Play bounce particles
            if (bounceParticles != null)
            {
                bounceParticles.Play();
            }
            
            soundData?.PlayAtPosition(jumpPad.transform.position);
            
            // Change sprite to disabled color during cooldown
            if (spriteRenderer != null)
            {
                spriteRenderer.color = disabledColor;
            }
        }
        
        private void OnCooldownComplete()
        {
            // Play ready animation
            if (readyTween != null)
            {
                readyTween.Play();
            }
            
            // Play ready particles
            if (readyParticles != null)
            {
                readyParticles.Play();
            }
            
            // Reset sprite color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }
    }
}
