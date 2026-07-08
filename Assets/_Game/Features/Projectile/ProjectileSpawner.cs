using System;
using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    public class ProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private float rate;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform directionPoint;
        [SerializeField] private float speed;
        [SerializeField] private float speedRotation;
        
        public Action<float> OnStartCharge { get; set; }
        public Action OnCharged { get; set; }

        private float _timer;
        private bool _isCharging;

        private void Update()
        {
            _timer += Time.deltaTime;

            if (!_isCharging && _timer >= rate)
            {
                _isCharging = true;
                _timer = 0f;
                OnStartCharge?.Invoke(rate);
            }

            if (_isCharging && _timer >= rate)
            {
                _isCharging = false;
                _timer = 0f;
                OnCharged?.Invoke();
                var instantiate = Instantiate(projectilePrefab, transform);
                var projectile = instantiate.GetComponent<Projectile>();
                projectile.transform.SetParent(SceneRoot.Instance.transform, true);
                projectile.Direction = (directionPoint.position - transform.position).normalized;
                projectile.Speed = speed;
                projectile.SpeedRotation = speedRotation;
            }
        }
    }
}