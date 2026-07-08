using Core._.UI;
using Light_and_controller.Scripts.Components.Enemy;
using UnityEngine;

namespace Light_and_controller.Scripts.UI
{
    public class SlimeView : MonoBehaviour
    {
        [SerializeField] private SlimeEnemy slimeEnemy;
        [SerializeField] private TweenableBase chargeTween;
        [SerializeField] private TweenableBase attackTween;
        
        private void OnEnable()
        {
            if (slimeEnemy != null)
            {
                slimeEnemy.OnChargeStart += HandleChargeStart;
                slimeEnemy.OnChargeStop += HandleChargeStop;
                slimeEnemy.OnAttack += HandleAttack;
            }
        }
        
        private void OnDisable()
        {
            if (slimeEnemy != null)
            {
                slimeEnemy.OnChargeStart -= HandleChargeStart;
                slimeEnemy.OnChargeStop -= HandleChargeStop;
                slimeEnemy.OnAttack -= HandleAttack;
            }
        }
        
        private void HandleChargeStart(float chargeDuration)
        {
            if (chargeTween != null)
            {
                chargeTween.Play();
                chargeTween.SetDuration(chargeDuration);
            }
        }
        
        private void HandleChargeStop()
        {
            if (chargeTween != null)
            {
                chargeTween.CompleteTween();
            }
        }
        
        private void HandleAttack()
        {
            if (attackTween != null)
            {
                attackTween.Play();
            }
        }
    }
}