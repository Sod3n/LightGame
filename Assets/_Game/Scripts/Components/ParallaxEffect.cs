using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    public class ParallaxEffect : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Vector2 parallaxMultiplier = new Vector2(0.5f, 0.5f);
        
        [Header("Options")]
        [SerializeField] private bool autoFindCamera = true;
        [SerializeField] private bool infiniteHorizontal = false;
        [SerializeField] private bool infiniteVertical = false;
        
        [Header("Activation Distance")]
        [SerializeField] private bool useActivationDistance = false;
        [SerializeField] private float activationDistanceX = 10f;
        [SerializeField] private float activationDistanceY = 10f;
        
        [Header("Infinite Scroll Settings")]
        [SerializeField] private float textureUnitSizeX;
        [SerializeField] private float textureUnitSizeY;
        
        private Vector3 _startPosition;
        private Vector3 _startCameraPosition;

        private void Start()
        {
            if (autoFindCamera && cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
                if (cameraTransform == null)
                {
                    Debug.LogWarning("ParallaxEffect: No camera found. Please assign a camera manually.");
                    enabled = false;
                    return;
                }
            }

            _startPosition = transform.position;
            _startCameraPosition = cameraTransform.position;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            Vector3 cameraDelta = cameraTransform.position - _startCameraPosition;
            
            float effectiveDeltaX = cameraDelta.x;
            float effectiveDeltaY = cameraDelta.y;

            if (useActivationDistance)
            {
                Vector3 objectToCamera = cameraTransform.position - transform.position;
                
                if (Mathf.Abs(objectToCamera.x) > activationDistanceX)
                {
                    effectiveDeltaX = 0f;
                }

                if (Mathf.Abs(objectToCamera.y) > activationDistanceY)
                {
                    effectiveDeltaY = 0f;
                }
            }

            Vector3 targetPosition = _startPosition + new Vector3(
                effectiveDeltaX * parallaxMultiplier.x,
                effectiveDeltaY * parallaxMultiplier.y,
                0f
            );

            if (infiniteHorizontal && textureUnitSizeX > 0)
            {
                float distanceFromStart = targetPosition.x - _startPosition.x;
                if (Mathf.Abs(distanceFromStart) >= textureUnitSizeX)
                {
                    float offsetX = distanceFromStart % textureUnitSizeX;
                    _startPosition.x += distanceFromStart - offsetX;
                    targetPosition.x = _startPosition.x + offsetX;
                }
            }

            if (infiniteVertical && textureUnitSizeY > 0)
            {
                float distanceFromStart = targetPosition.y - _startPosition.y;
                if (Mathf.Abs(distanceFromStart) >= textureUnitSizeY)
                {
                    float offsetY = distanceFromStart % textureUnitSizeY;
                    _startPosition.y += distanceFromStart - offsetY;
                    targetPosition.y = _startPosition.y + offsetY;
                }
            }

            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        }

        public void SetParallaxMultiplier(Vector2 multiplier)
        {
            parallaxMultiplier = multiplier;
        }

        public void SetCamera(Transform camera)
        {
            cameraTransform = camera;
            if (cameraTransform != null)
            {
                _startCameraPosition = cameraTransform.position;
            }
        }
    }
}
