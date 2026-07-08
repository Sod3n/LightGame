using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core._.UI
{
    public class Light2DIntensityTween : TweenableBase
    {
        [SerializeField] private Light2D light = null;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private float targetIntensity = 1f;

        public float Duration
        {
            get => duration;
            set => duration = value;
        }
        
        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();
            
            sequence.Append(DOVirtual.Float(light.intensity, targetIntensity, duration, t => light.intensity = t).SetEase(showEase));
            return sequence;
        }
        
    }
}