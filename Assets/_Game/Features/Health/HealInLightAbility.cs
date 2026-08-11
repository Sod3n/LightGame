using LightGame.Features.Abilities;
using UnityEngine;

namespace LightGame.Features
{
    [CreateAssetMenu(fileName = "HealInLight", menuName = "Abilities/HealInLight")]
    public class HealInLightAbility : LevelAbility
    {
        [SerializeField] private HealOverTimeEffect.EffectData healData = new HealOverTimeEffect.EffectData
        {
            Amount = 1,
            Rate = 1f,
            IsInfinity = true,
            ResetTickOnHealthChange = true
        };

        public override void Activate(GameObject target)
        {
            if (target.GetComponent<LightDetector>() == null)
                target.AddComponent<LightDetector>();

            var reactive = target.GetComponent<LightReactiveEffect>() ?? target.AddComponent<LightReactiveEffect>();
            reactive.SetOnLight(healData);
            reactive.enabled = true;
            Debug.Log("Heal in Light ability activated!");
        }

        public override void Deactivate(GameObject target)
        {
            var reactive = target.GetComponent<LightReactiveEffect>();
            if (reactive == null) return;
            reactive.SetOnLight(null);
            if (!reactive.AnyActive) reactive.enabled = false;
            Debug.Log("Heal in Light ability deactivated");
        }
    }
}
