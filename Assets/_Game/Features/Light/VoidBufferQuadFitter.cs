using UnityEngine;

namespace LightGame.Features
{
    [RequireComponent(typeof(MeshRenderer))]
    public class VoidBufferQuadFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float distanceFromCamera = 5f;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = GetComponentInParent<Camera>();
        }

        private void OnEnable()
        {
            if (targetCamera == null)
                targetCamera = GetComponentInParent<Camera>();

            Fit();
        }

        private void LateUpdate()
        {
            Fit();
        }

        private void Fit()
        {
            if (targetCamera == null) return;

            transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
            transform.localRotation = Quaternion.identity;

            var height = targetCamera.orthographicSize * 2f;
            var width = height * targetCamera.aspect;
            transform.localScale = new Vector3(width, height, 1f);
        }
    }
}
