using UnityEngine;

namespace LightGame.Features
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteBlur : MonoBehaviour
    {
        [SerializeField] private float blurPixels;

        private SpriteRenderer _sr;
        private MaterialPropertyBlock _mpb;
        private static readonly int BlurPixelsId = Shader.PropertyToID("_BlurPixels");

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        private void Apply()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(BlurPixelsId, blurPixels);
            _sr.SetPropertyBlock(_mpb);
        }
    }
}
