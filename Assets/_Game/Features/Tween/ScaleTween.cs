
using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Tween for showing a hint popup from the bottom of the screen using slide animation.
    /// The popup slides up from below the screen with an optional bounce effect.
    /// </summary>
    public class ScaleTween : TweenableBase
    {
        [SerializeField] private bool _useStartScale;
        [SerializeField] private Vector3 _startScale;
        [SerializeField] private Vector3 _endScale;
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private Ease _scaleEase = Ease.OutElastic;

        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            var parent = transform.parent;
            var startScale = _useStartScale ? _startScale : parent.localScale;
            
            if (_useStartScale)
            {
                parent.localScale = _startScale;
            }
            
            return DOVirtual.Float(0f, 1f, _duration, value =>
            {
                parent.localScale = Vector3.Lerp(startScale, _endScale, value);
            }).SetEase(_scaleEase);
        }
    }
}
