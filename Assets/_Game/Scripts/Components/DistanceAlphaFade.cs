using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Light_and_controller.Scripts.Components
{
    public class DistanceAlphaFade : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private string playerTag = "Player";
        
        [Header("Distance Settings")]
        [SerializeField] private float minDistance = 0f;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [Header("Alpha Settings")]
        [SerializeField] private float minAlpha = 0f;
        [SerializeField] private float maxAlpha = 1f;
        
        [Header("Performance")]
        [SerializeField] private bool updateInFixedUpdate = false;
        [SerializeField] private float updateInterval = 0.1f;
        
        [Header("Tween Settings")]
        [SerializeField] private float tweenDuration = 0.3f;
        [SerializeField] private Ease tweenEase = Ease.OutQuad;
        
        private Transform _playerTransform;
        private TMP_Text _tmpText;
        private Image _image;
        private SpriteRenderer _spriteRenderer;
        private float _lastUpdateTime;
        private Color _originalColor;
        private bool _hasValidComponent;
        private Tween _alphaTween;
        private float _currentAlpha;

        private void Awake()
        {
            _tmpText = GetComponent<TMP_Text>();
            _image = GetComponent<Image>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            _hasValidComponent = _tmpText != null || _image != null || _spriteRenderer != null;
            
            if (!_hasValidComponent)
            {
                Debug.LogWarning($"DistanceAlphaFade on {gameObject.name}: No TMP_Text, Image, or SpriteRenderer component found!");
                enabled = false;
                return;
            }
            
            if (_tmpText != null)
            {
                _originalColor = _tmpText.color;
                _currentAlpha = _tmpText.color.a;
            }
            else if (_image != null)
            {
                _originalColor = _image.color;
                _currentAlpha = _image.color.a;
            }
            else if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                _currentAlpha = _spriteRenderer.color.a;
            }
        }
        
        private void OnDestroy()
        {
            _alphaTween?.Kill();
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
                Debug.LogWarning($"DistanceAlphaFade on {gameObject.name}: Player with tag '{playerTag}' not found!");
            }
        }

        private void Update()
        {
            if (!updateInFixedUpdate)
            {
                UpdateAlpha();
            }
        }

        private void FixedUpdate()
        {
            if (updateInFixedUpdate)
            {
                UpdateAlpha();
            }
        }

        private void UpdateAlpha()
        {
            if (!_hasValidComponent)
                return;
            
            if (Time.time - _lastUpdateTime < updateInterval)
                return;
            
            _lastUpdateTime = Time.time;
            
            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null)
                    return;
            }
            
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distance);
            float curveValue = fadeCurve.Evaluate(normalizedDistance);
            float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, curveValue);
            
            if (Mathf.Abs(_currentAlpha - targetAlpha) > 0.01f)
            {
                _alphaTween?.Kill();
                _alphaTween = DOTween.To(() => _currentAlpha, x => _currentAlpha = x, targetAlpha, tweenDuration)
                    .SetEase(tweenEase)
                    .OnUpdate(() => ApplyAlpha(_currentAlpha))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }
        
        private void ApplyAlpha(float alpha)
        {
            Color newColor = _originalColor;
            newColor.a = alpha;
            
            if (_tmpText != null)
            {
                _tmpText.color = newColor;
            }
            else if (_image != null)
            {
                _image.color = newColor;
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.color = newColor;
            }
        }

        private void OnValidate()
        {
            if (minDistance > maxDistance)
            {
                minDistance = maxDistance;
            }
            
            minAlpha = Mathf.Clamp01(minAlpha);
            maxAlpha = Mathf.Clamp01(maxAlpha);
            updateInterval = Mathf.Max(0f, updateInterval);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, minDistance);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}
