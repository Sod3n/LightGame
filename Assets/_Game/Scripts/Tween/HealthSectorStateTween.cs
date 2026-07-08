using System;
using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Unified tween for health sector animations with different state-based behaviors.
    /// Supports heal, damage, pending heal, and pending damage states with configurable parameters.
    /// </summary>
    public class HealthSectorStateTween : TweenableBase
    {
        public enum AnimationState
        {
            Heal,
            Damage,
            PendingHeal,
            PendingDamage,
            CancelPendingHeal,
            CancelPendingDamage
        }

        [Header("Animation State")]
        [SerializeField] private AnimationState currentState = AnimationState.Heal;
        
        [Header("Tweens")]
        [SerializeField] private TweenableBase healTween;
        [SerializeField] private TweenableBase damageTween;
        [SerializeField] private TweenableBase pendingHealTween;
        [SerializeField] private TweenableBase pendingDamageTween;

        [Header("Settings")]
        [SerializeField] private float defaultCancelDuration = 0.5f;

        private float _customDuration = -1f;
        
        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            // For editor preview, return the appropriate tween based on current state
            // This allows the CustomTweenAnimationEditor to preview the animation
            switch (currentState)
            {
                case AnimationState.Heal:
                    return healTween?.CreateTween() ?? DOTween.Sequence();
                case AnimationState.Damage:
                    return damageTween?.CreateTween() ?? DOTween.Sequence();
                case AnimationState.PendingHeal:
                    return pendingHealTween?.CreateTween() ?? DOTween.Sequence();
                case AnimationState.PendingDamage:
                    return pendingDamageTween?.CreateTween() ?? DOTween.Sequence();
                case AnimationState.CancelPendingHeal:
                case AnimationState.CancelPendingDamage:
                    // For cancel states, return the pending tween in reverse
                    var cancelTween = currentState == AnimationState.CancelPendingHeal 
                        ? pendingHealTween?.CreateTween() 
                        : pendingDamageTween?.CreateTween();
                    if (cancelTween != null)
                    {
                        cancelTween.SetInverted(true);
                    }
                    return cancelTween ?? DOTween.Sequence();
                default:
                    return DOTween.Sequence();
            }
        }
        
        public override void Play()
        {
            // Execute the appropriate animation based on current state
            switch (currentState)
            {
                case AnimationState.Heal:
                    ExecuteHeal();
                    break;
                case AnimationState.Damage:
                    ExecuteDamage();
                    break;
                case AnimationState.PendingHeal:
                    ExecutePendingHeal();
                    break;
                case AnimationState.PendingDamage:
                    ExecutePendingDamage();
                    break;
                case AnimationState.CancelPendingHeal:
                    ExecuteCancelPendingHeal();
                    break;
                case AnimationState.CancelPendingDamage:
                    ExecuteCancelPendingDamage();
                    break;
            }
        }

        /// <summary>
        /// Set the animation state and optional duration, then call Play() to execute.
        /// </summary>
        public void SetState(AnimationState state, float duration = -1f)
        {
            currentState = state;
            _customDuration = duration;
        }

        private void ExecuteHeal()
        {
            ResetDurations();
            Reset();

            PlayForward(healTween);
        }

        private void ExecuteDamage()
        {
            ResetDurations();
            this.DOPause();

            PlayForward(damageTween);
        }

        private void ExecutePendingHeal()
        {
            ResetDurations();
            Reset();
            
            PlayForward(pendingHealTween);
            pendingHealTween.SetDuration(_customDuration);
        }

        private void ExecutePendingDamage()
        {
            ResetDurations();
            Reset();
            
            PlayForward(pendingDamageTween);
            pendingDamageTween.SetDuration(_customDuration);
        }

        private void ExecuteCancelPendingHeal()
        {
            ResetDurations();
            
            pendingHealTween.Tween?.PlayBackwards();
            pendingHealTween.SetDuration(defaultCancelDuration);
        }

        private void ExecuteCancelPendingDamage()
        {
            ResetDurations();
            
            pendingDamageTween.Tween?.PlayBackwards();
            pendingDamageTween.SetDuration(defaultCancelDuration);
        }

        private void PlayForward(TweenableBase tweenBase)
        {
            tweenBase.Tween = tweenBase.CreateTween();
            tweenBase.Tween.SetTarget(this);
            tweenBase.Tween.PlayForward();
            tweenBase.Tween.SetAutoKill(false);
        }

        private void Reset()
        {
            pendingDamageTween.Tween?.Rewind();
            pendingHealTween.Tween?.Rewind();
            this.DOKill();
        }

        private void ResetDurations()
        {
            if (pendingHealTween?.Tween != null)
                pendingHealTween.SetDuration(pendingHealTween.Tween.Duration());
            if (pendingDamageTween?.Tween != null)
                pendingDamageTween.SetDuration(pendingDamageTween.Tween.Duration());
            if (healTween?.Tween != null)
                healTween.SetDuration(healTween.Tween.Duration());
            if (damageTween?.Tween != null)
                damageTween.SetDuration(damageTween.Tween.Duration());
        }
    }
}
