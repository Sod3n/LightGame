using Core._.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Light_and_controller.Scripts.Components;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

namespace Light_and_controller.Scripts.UI
{
    public class ProjectileSpawnerView : MonoBehaviour
    {
        [SerializeField] private ProjectileSpawner projectileSpawner = null;
        [SerializeField] private Light2DIntensityTween chargeTween = null;
        [SerializeField] private Light2DIntensityTween unchargeTween = null;
        
        private bool _isUncharging = false;
        private float _unchargeDuration = 0f;
        
        private void Awake()
        {
            projectileSpawner ??= GetComponent<ProjectileSpawner>();
        }
        
        private void OnEnable()
        {
            projectileSpawner.OnStartCharge += OnStartCharge;
            projectileSpawner.OnCharged += OnCharged;
        }
        
        private void OnDisable()
        {
            projectileSpawner.OnStartCharge -= OnStartCharge;
            projectileSpawner.OnCharged -= OnCharged;
        }
        
        private async void OnStartCharge(float rate)
        {
            await UniTask.WaitUntil(() => !_isUncharging);
            chargeTween.Duration = rate - _unchargeDuration;
            chargeTween.Play();
        }
        
        private void OnCharged()
        {
            _isUncharging = true;
            unchargeTween.Play();
            unchargeTween.Tween.OnComplete(() => _isUncharging = false);
            _unchargeDuration = unchargeTween.Duration;
 
        }
    }
}