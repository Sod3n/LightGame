
using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    /// <summary>
    /// Composes multiple TweenableBase components into a single sequence.
    /// Allows combining different tweens to play in sequence or parallel.
    /// </summary>
    public class ComposableTween : TweenableBase
    {
        [SerializeField] private TweenableBase[] _tweens;
        [SerializeField] private bool _playInParallel = false;

        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            var sequence = DOTween.Sequence();

            if (_tweens == null || _tweens.Length == 0)
            {
                Debug.LogWarning("ComposableTween: No tweens assigned!", this);
                return sequence;
            }

            foreach (var tweenableBase in _tweens)
            {
                if (tweenableBase == null) continue;

                var childTween = tweenableBase.CreateTween();
                if (childTween == null) continue;

                if (_playInParallel)
                {
                    sequence.Join(childTween);
                }
                else
                {
                    sequence.Append(childTween);
                }
            }

            return sequence;
        }
    }
}
