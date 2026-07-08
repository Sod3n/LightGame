using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    public class DistanceAnimationControl : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private string playerTag = "Player";
        
        [Header("Distance Settings")]
        [SerializeField] private float activationRadius = 5f;
        
        [Header("Animation Settings")]
        [Tooltip("How fast the animation plays forward (1 = full clip in 1 second)")]
        [SerializeField] private float forwardSpeed = 1f;
        [Tooltip("How fast the animation plays backward (1 = full clip in 1 second)")]
        [SerializeField] private float backwardSpeed = 1f;
        
        [Header("Performance")]
        [SerializeField] private float updateInterval = 0.1f;
        
        private Transform _playerTransform;
        private Animator _animator;
        private float _lastCheckTime;
        private bool _isPlayerInside;
        private float _normalizedTime;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            
            if (_animator == null)
            {
                Debug.LogWarning($"DistanceAnimationControl on {gameObject.name}: No Animator component found!");
                enabled = false;
                return;
            }
            
            _animator.speed = 0f;
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogWarning($"DistanceAnimationControl on {gameObject.name}: Player with tag '{playerTag}' not found!");
            }
        }

        private void Update()
        {
            if (_animator == null)
                return;
            
            // Check distance at interval
            if (Time.time - _lastCheckTime >= updateInterval)
            {
                _lastCheckTime = Time.time;
                
                if (_playerTransform == null)
                {
                    FindPlayer();
                    if (_playerTransform == null)
                        return;
                }
                
                float distance = Vector3.Distance(transform.position, _playerTransform.position);
                _isPlayerInside = distance <= activationRadius;
            }
            
            // Scrub every frame for smooth playback
            float speed = _isPlayerInside ? forwardSpeed : -backwardSpeed;
            _normalizedTime = Mathf.Clamp(_normalizedTime + speed * Time.deltaTime, 0f, 0.95f);
            
            _animator.Play(0, 0, _normalizedTime);
            _animator.Update(0f);
        }

        private void OnValidate()
        {
            activationRadius = Mathf.Max(0f, activationRadius);
            updateInterval = Mathf.Max(0f, updateInterval);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, activationRadius);
        }
    }
}
