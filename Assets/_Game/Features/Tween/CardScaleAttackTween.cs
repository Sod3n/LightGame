using DG.Tweening;
using UnityEngine;

namespace Core._.UI
{
    public class CardScaleAttackTween : TweenableBase
    {
        // ========================================
        // AUTO-GENERATED CODE - DO NOT MODIFY
        // Generated from: Card_scale_attack
        // Generation time: 2025-10-10 05:49:45
        // ========================================

        [SerializeField] private GameObject view; // View
        [SerializeField] private GameObject border; // View/Face/facePivot/Border
        [SerializeField] private GameObject rune; // View/Face/facePivot/Rune
        [SerializeField] private GameObject rank; // View/Face/facePivot/Rank
        [SerializeField] private GameObject label; // View/Face/facePivot/Label
        [SerializeField] private GameObject damage1; // View/Face/facePivot/Mask/Damage (1)
        [SerializeField] private GameObject damage; // View/Face/facePivot/Mask/Damage
        [SerializeField] private GameObject effectSlot; // View/Face/facePivot/EffectSlot
        [SerializeField] private GameObject placeEffect; // PlaceEffect

        [SerializeField] private AnimationCurve xCurve_view_localScale = new AnimationCurve(new Keyframe(0f, 1.374488f, 0f, 0f), new Keyframe(0.08333334f, 1.7593f, 0f, 0f), new Keyframe(0.45f, 1.7593f, 0f, 0f), new Keyframe(0.5f, 1.924904f, 0f, 0f), new Keyframe(0.55f, 1.564032f, -12.05039f, -12.05039f), new Keyframe(0.5833333f, 1.001255f, -10.04417f, 5.131155f), new Keyframe(0.65f, 1.162032f, 0f, 0.5600271f), new Keyframe(0.75f, 1f, -0.0961667f, 0f), new Keyframe(0.85f, 1f, 0f, 1.728456E-06f), new Keyframe(0.9166667f, 1f, 0f, 0f), new Keyframe(1.166667f, 1f, 0f, 1.728456E-06f), new Keyframe(2.5f, 1f, 0f, 1.728456E-06f));
        [SerializeField] private AnimationCurve yCurve_view_localScale = new AnimationCurve(new Keyframe(0f, 1.374488f, 0f, 0f), new Keyframe(0.08333334f, 1.7593f, 0f, 0f), new Keyframe(0.45f, 1.7593f, 0f, 0f), new Keyframe(0.5f, 1.924904f, 0f, 0f), new Keyframe(0.55f, 1.564032f, -12.06921f, -12.06921f), new Keyframe(0.5833333f, 1f, -10.25899f, 5.786962f), new Keyframe(0.65f, 1.170941f, 0f, 0.3264256f), new Keyframe(0.75f, 1f, -0.2671792f, 0f), new Keyframe(0.85f, 1f, 0f, 1.921778E-06f), new Keyframe(0.9166667f, 1f, 0f, 0f), new Keyframe(1.166667f, 1f, 0f, 1.921778E-06f), new Keyframe(2.5f, 1f, 0f, 1.921778E-06f));
        [SerializeField] private AnimationCurve zCurve_view_localScale = new AnimationCurve(new Keyframe(0f, 1.374488f, 0f, 0f), new Keyframe(0.08333334f, 1.7593f, 0f, 0f), new Keyframe(0.45f, 1.7593f, 0f, 0f), new Keyframe(0.5f, 1.924904f, 0f, 0f), new Keyframe(0.55f, 1.564032f, -11.95411f, -11.95411f), new Keyframe(0.5833333f, 1.007674f, -10.41278f, 5.350144f), new Keyframe(0.65f, 1.16166f, 0f, 0.624552f), new Keyframe(0.75f, 1f, 0.1163559f, 0f), new Keyframe(0.85f, 1f, 0f, 0f), new Keyframe(0.9166667f, 1f, 0f, 0f), new Keyframe(1.166667f, 1f, 0f, 0f), new Keyframe(2.5f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve xCurve_view_localEulerAngles = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.1666667f, 0.297f, 0f, 0f), new Keyframe(0.4f, 0.173f, -0.6845454f, -0.6845454f), new Keyframe(0.5333334f, 0.046f, -0.9436365f, -0.9436365f), new Keyframe(0.5833333f, 0f, -1.38f, -1.38f), new Keyframe(0.6666667f, -11.23285f, 0f, 0f), new Keyframe(0.6833333f, -10.34985f, 35.32003f, 35.32003f), new Keyframe(0.7666667f, 0f, 0f, 0f));
        [SerializeField] private AnimationCurve yCurve_view_localEulerAngles = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.1666667f, 0.9235983f, 0f, 0f), new Keyframe(0.4f, -0.4135427f, 0f, 0f), new Keyframe(0.5333334f, -0.111f, 2.255688f, 2.255688f), new Keyframe(0.5833333f, 0f, 0f, 0f), new Keyframe(0.6166667f, 0f, 0f, 0f), new Keyframe(0.65f, 1.104f, 0f, 0f), new Keyframe(0.7666667f, 0f, 0f, 0f));
        [SerializeField] private AnimationCurve zCurve_view_localEulerAngles = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.1666667f, -0.3919409f, 0f, 0f), new Keyframe(0.4f, 0.8611443f, 0f, 0f), new Keyframe(0.5f, -23.25132f, 0f, 0f), new Keyframe(0.5833333f, 0f, 0f, 0f), new Keyframe(0.6166667f, 0f, 0f, 0f), new Keyframe(0.65f, -3.697f, 0f, 0f), new Keyframe(0.7666667f, 0f, 0f, 0f));
        [SerializeField] private AnimationCurve xCurve_border_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6666667f, 0.93813f, 0f, 0f), new Keyframe(0.7333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve yCurve_border_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6666667f, 0.93813f, 0f, 0f), new Keyframe(0.7333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve zCurve_border_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6666667f, 0.93813f, 0f, 0f), new Keyframe(0.7333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve xCurve_rune_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6166667f, 0.77018f, 0f, 0f), new Keyframe(0.7333333f, 1.246767f, 0f, 0f), new Keyframe(0.8333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve yCurve_rune_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6166667f, 0.77018f, 0f, 0f), new Keyframe(0.7333333f, 1.246767f, 0f, 0f), new Keyframe(0.8333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve zCurve_rune_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6166667f, 0.77018f, 0f, 0f), new Keyframe(0.7333333f, 1.246767f, 0f, 0f), new Keyframe(0.8333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve xCurve_rank_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6166667f, 0.77018f, 0f, 0f), new Keyframe(0.7333333f, 1.246767f, 0f, 0f), new Keyframe(0.8333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve yCurve_rank_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6166667f, 0.77018f, 0f, 0f), new Keyframe(0.7333333f, 1.246767f, 0f, 0f), new Keyframe(0.8333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve zCurve_rank_localScale = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.5833333f, 1f, 0f, 0f), new Keyframe(0.6166667f, 0.77018f, 0f, 0f), new Keyframe(0.7333333f, 1.246767f, 0f, 0f), new Keyframe(0.8333333f, 1f, 0f, 0f));
        [SerializeField] private AnimationCurve xCurve_label_localScale = new AnimationCurve(new Keyframe(0f, 0.115f, 0f, 0f), new Keyframe(0.5833333f, 0.115f, 0f, 0f), new Keyframe(0.65f, 0.09f, 0f, 0f), new Keyframe(0.75f, 0.14f, 0f, 0f), new Keyframe(0.8666667f, 0.115f, 0f, 0f));
        [SerializeField] private AnimationCurve yCurve_label_localScale = new AnimationCurve(new Keyframe(0f, 0.115f, 0f, 0f), new Keyframe(0.5833333f, 0.115f, 0f, 0f), new Keyframe(0.65f, 0.09f, 0f, 0f), new Keyframe(0.75f, 0.14f, 0f, 0f), new Keyframe(0.8666667f, 0.115f, 0f, 0f));
        [SerializeField] private AnimationCurve zCurve_label_localScale = new AnimationCurve(new Keyframe(0f, 0.115f, 0f, 0f), new Keyframe(0.5833333f, 0.115f, 0f, 0f), new Keyframe(0.65f, 0.09f, 0f, 0f), new Keyframe(0.75f, 0.14f, 0f, 0f), new Keyframe(0.8666667f, 0.115f, 0f, 0f));

        public override Tween Tween { get; set; }

        public override Tween CreateTween()
        {
            // Null checks
            if (view == null) { Debug.LogError("view is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }
            if (border == null) { Debug.LogError("border is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }
            if (rune == null) { Debug.LogError("rune is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }
            if (rank == null) { Debug.LogError("rank is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }
            if (label == null) { Debug.LogError("label is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }
            if (damage1 == null) { Debug.LogError("damage1 is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }
            if (damage == null) { Debug.LogError("damage is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }
            if (effectSlot == null) { Debug.LogError("effectSlot is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }
            if (placeEffect == null) { Debug.LogError("placeEffect is not assigned in Card_scale_attack!"); return DOTween.Sequence(); }

            var sequence = DOTween.Sequence();
            float duration = 2.5f;

            // Animate localScale
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                view.transform.localScale = new Vector3(xCurve_view_localScale.Evaluate(time), yCurve_view_localScale.Evaluate(time), zCurve_view_localScale.Evaluate(time));
            }));

            // Animate localEulerAngles
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                view.transform.localEulerAngles = new Vector3(xCurve_view_localEulerAngles.Evaluate(time), yCurve_view_localEulerAngles.Evaluate(time), zCurve_view_localEulerAngles.Evaluate(time));
            }));

            // Animate localScale
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                border.transform.localScale = new Vector3(xCurve_border_localScale.Evaluate(time), yCurve_border_localScale.Evaluate(time), zCurve_border_localScale.Evaluate(time));
            }));

            // Animate localScale
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                rune.transform.localScale = new Vector3(xCurve_rune_localScale.Evaluate(time), yCurve_rune_localScale.Evaluate(time), zCurve_rune_localScale.Evaluate(time));
            }));

            // Animate localScale
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                rank.transform.localScale = new Vector3(xCurve_rank_localScale.Evaluate(time), yCurve_rank_localScale.Evaluate(time), zCurve_rank_localScale.Evaluate(time));
            }));

            // Animate localScale
            sequence.Join(DOVirtual.Float(0f, 1f, duration, t => {
                float time = t * duration;
                label.transform.localScale = new Vector3(xCurve_label_localScale.Evaluate(time), yCurve_label_localScale.Evaluate(time), zCurve_label_localScale.Evaluate(time));
            }));

            sequence.InsertCallback(0f, () => damage1.SetActive(false));

            sequence.InsertCallback(0f, () => damage.SetActive(false));

            sequence.InsertCallback(0f, () => effectSlot.SetActive(true));
            sequence.InsertCallback(2.5f, () => effectSlot.SetActive(false));

            sequence.InsertCallback(0f, () => placeEffect.SetActive(false));
            sequence.InsertCallback(0.03333334f, () => placeEffect.SetActive(true));
            sequence.InsertCallback(2.5f, () => placeEffect.SetActive(false));


            return sequence;
        }
    }
}