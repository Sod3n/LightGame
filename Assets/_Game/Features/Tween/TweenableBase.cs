using System;
using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    public abstract class TweenableBase : MonoBehaviour
    {
        [SerializeField] private bool playOnEnable;

        protected virtual void OnEnable()
        {
           if(playOnEnable) Play();
        }

        public abstract Tween Tween { get; set; }
        public abstract Tween CreateTween();

        public virtual void Play()
        {
            Tween?.Complete();
            Tween = CreateTween();
        }
        
        public virtual void PlayInverted(bool inverted)
        {
            Tween?.Complete();
            Tween = CreateTween();
            Tween.SetInverted(inverted);
        }
        
        public void CompleteTween() => Tween?.Complete();
        
        public void KillAll() => transform?.DOKill();
        
        public void SetDuration(float duration)
        {
            if (Tween != null && Tween.IsActive())
            {
                Tween.timeScale = Tween.Duration() / duration;
            }
        }
        
        public virtual void Revert()
        {
            // Override in derived classes to implement revert behavior
        }
    }
}