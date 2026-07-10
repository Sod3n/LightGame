using LightGame.Features;
using UnityEngine;

using LightGame.Core;
namespace LightGame.Features.Abilities
{
    [CreateAssetMenu(fileName = "DamageInDark", menuName = "Abilities/DamageInDark")]
    public class DamageInDarkAbility : LevelAbility
    {
        [SerializeField] private DamageOverTimeEffect.EffectData damageData = new DamageOverTimeEffect.EffectData
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
            reactive.SetOnDark(damageData);
            reactive.enabled = true;
            Debug.Log("Damage in Dark ability activated!");
        }

        public override void Deactivate(GameObject target)
        {
            var reactive = target.GetComponent<LightReactiveEffect>();
            if (reactive == null) return;
            reactive.SetOnDark(null);
            if (!reactive.AnyActive) reactive.enabled = false;
            Debug.Log("Damage in Dark ability deactivated");
        }
    }
}
