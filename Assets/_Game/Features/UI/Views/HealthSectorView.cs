using System;
using Core._.UI;
using DG.Tweening;
using UnityEngine;

namespace Light_and_controller.Scripts.UI
{
    public class HealthSectorView : MonoBehaviour
    {
        [SerializeField] private HealthSectorStateTween stateTween;

        public bool IsFilled { get; set; } = true;
        private bool _wasPendingToDamage = false;
        private bool _wasPendingToHeal = false;


        public void OnHeal()
        {
            stateTween.SetState(HealthSectorStateTween.AnimationState.Heal);
            stateTween.Play();

            IsFilled = true;
            Debug.Log($"OnHeal {gameObject.name}");
        }

        public void OnDamage()
        {
            stateTween.SetState(HealthSectorStateTween.AnimationState.Damage);
            stateTween.Play();
            
            IsFilled = false;
            Debug.Log($"OnDamage {gameObject.name}");
        }
        
        public void OnHealPending(float duration)
        {
            stateTween.SetState(HealthSectorStateTween.AnimationState.PendingHeal, duration);
            stateTween.Play();
            
            Debug.Log($"OnHealPending {gameObject.name}");
        }

        public void OnHealPendingCancel()
        {
            stateTween.SetState(HealthSectorStateTween.AnimationState.CancelPendingHeal);
            stateTween.Play();
            
            Debug.Log($"OnHealPendingCancel {gameObject.name}");
        }

        public void OnTakeDamagePending(float duration)
        {
            stateTween.SetState(HealthSectorStateTween.AnimationState.PendingDamage, duration);
            stateTween.Play();
            
            Debug.Log($"OnTakeDamagePending {gameObject.name}");
        }
        
        public void OnTakeDamagePendingCancel()
        {
            stateTween.SetState(HealthSectorStateTween.AnimationState.CancelPendingDamage);
            stateTween.Play();
            
            Debug.Log($"OnTakeDamagePendingCancel {gameObject.name}");
        }
    }
}