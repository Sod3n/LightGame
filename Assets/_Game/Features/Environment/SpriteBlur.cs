using UnityEngine;

namespace LightGame.Features
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteBlur : MonoBehaviour
    {
        [SerializeField] private float blurPixels;
        [Tooltip("Extra LOD added at a fully-transparent edge (1 = 2x blur radius near edges).")]
        [SerializeField] private float edgeBlurBoost = 2f;

        private SpriteRenderer _sr;
        private MaterialPropertyBlock _mpb;
        private static readonly int BlurPixelsId    = Shader.PropertyToID("_BlurPixels");
        private static readonly int EdgeBlurBoostId = Shader.PropertyToID("_EdgeBlurBoost");

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        private void Apply()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(BlurPixelsId,    blurPixels);
            _mpb.SetFloat(EdgeBlurBoostId, edgeBlurBoost);
            _sr.SetPropertyBlock(_mpb);
        }
    }
}
