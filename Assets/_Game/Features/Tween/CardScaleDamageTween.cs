using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    public class CardScaleDamageTween : TweenableBase
    {
        // ========================================
        // AUTO-GENERATED CODE - DO NOT MODIFY
        // Generated from: Card_scale_damage
        // Generation time: 2025-10-10 05:45:24
        // ========================================

        [SerializeField] private GameObject view; // View
        [SerializeField] private GameObject damage1; // View/Face/facePivot/Mask/Damage (1)
        [SerializeField] private GameObject damage; // View/Face/facePivot/Mask/Damage
        [SerializeField] private GameObject art; // View/Face/facePivot/Mask/Art
        [SerializeField] private Animator animator;

        [SerializeField] private AnimationCurve yCurve_view_anchoredPosition = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.3333333f, 0f, 0f, 0f), new Keyframe(0.4f, 0f, 0f, 0f), new Keyframe(1.066667f, 0f, 0f, 0f), new Keyframe(2.333333f, 0f, 0f, 0f));
        [SerializeField] private AnimationCurve xCurve_view_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.3333333f, 1f, 0f, 0f), new Keyframe(0.4f, 1.048929f, 0f, 0f), new Keyframe(0.4666667f, 0.9149386f, 0f, 0f), new Keyframe(0.55f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve yCurve_view_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.3333333f, 1f, 0f, 0f), new Keyframe(0.4f, 0.9371633f, 0f, 0f), new Keyframe(0.4666667f, 1.090847f, 0f, 0f), new Keyframe(0.55f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve zCurve_view_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.3333333f, 1f, 0f, 0f), new Keyframe(0.4f, 1f, 0f, 0f), new Keyframe(0.4666667f, 1f, 0f, 0f), new Keyframe(0.55f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve aCurve_art_color = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.3333333f, 1f, 0f, 0f), new Keyframe(0.3833333f, 0f, 0f, 0f), new Keyframe(0.4166667f, 1f, 0f, 0f));
        [SerializeField] private float delay;
        
        public GameObject Damage1 { get => damage1; set => damage1 = value; }
        public GameObject Damage { get => damage; set => damage = value; }
        
        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            // Null checks
            if (view == null) { Debug.LogError("view is not assigned in Card_scale_damage!"); return DOTween.Sequence(); }
            if (damage1 == null) { Debug.LogError("damage1 is not assigned in Card_scale_damage!"); return DOTween.Sequence(); }
            if (damage == null) { Debug.LogError("damage is not assigned in Card_scale_damage!"); return DOTween.Sequence(); }
            if (art == null) { Debug.LogError("art is not assigned in Card_scale_damage!"); return DOTween.Sequence(); }
            if (animator == null) { Debug.LogError("art is not assigned in Card_scale_damage!"); return DOTween.Sequence(); }

            var sequence = DOTween.Sequence();
            float duration = 2.333333f;

            // Animate anchoredPosition
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                view.GetComponent<RectTransform>().anchoredPosition = new Vector2(view.GetComponent<RectTransform>().anchoredPosition.x, yCurve_view_anchoredPosition.Evaluate(time));
            }));

            // Animate localScale
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                view.transform.localScale = new Vector3(xCurve_view_localScale.Evaluate(time), yCurve_view_localScale.Evaluate(time), zCurve_view_localScale.Evaluate(time));
            }));

            sequence.InsertCallback(0f, () => damage1.SetActive(false));
            sequence.InsertCallback(0.4166667f, () => damage1.SetActive(true));

            sequence.InsertCallback(0f, () => damage.SetActive(false));
            sequence.InsertCallback(0.4166667f, () => damage.SetActive(true));

            // Animate color
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                art.GetComponent<UnityEngine.UI.Image>().color = new Color(art.GetComponent<UnityEngine.UI.Image>().color.r, art.GetComponent<UnityEngine.UI.Image>().color.g, art.GetComponent<UnityEngine.UI.Image>().color.b, aCurve_art_color.Evaluate(time));
            }));

            var startState = animator.enabled;
            sequence.InsertCallback(0f, () => animator.enabled = false);
            sequence.OnComplete(() => animator.enabled = startState);

            sequence.SetDelay(delay);
            
            return sequence;
        }
    }
}