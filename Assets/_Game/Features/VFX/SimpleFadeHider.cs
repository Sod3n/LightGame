using System.Collections;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

/// <summary>
/// Simple alpha fade approach - works with any material.
/// Good for quick prototyping without custom shaders.
/// </summary>
[RequireComponent(typeof(LightDetector))]
public class SimpleFadeHider : MonoBehaviour, ILightable
{
    [Header("References")]
    [SerializeField] private MeshRenderer m_Renderer;
    [SerializeField] private SpriteRenderer m_SpriteRenderer; // For 2D sprites

    [Header("Fade Settings")]
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private float physicsDisableAlpha = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Physics Settings")]
    [SerializeField] private bool checkForPlayerOnTop = true;

    private Material _material;
    private int _originalLayer;
    private float _currentAlpha = 1f;
    private float _targetAlpha = 1f;
    private Coroutine _fadeCoroutine;
    private Collider2D _collider;
    private bool _physicsEnabled = true;

    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        _originalLayer = gameObject.layer;
        _collider = GetComponent<Collider2D>();

        // Get material
        if (m_Renderer != null)
        {
            _material = m_Renderer.material;
        }
        else if (m_SpriteRenderer != null)
        {
            _material = m_SpriteRenderer.material;
        }

        if (_material != null)
        {
            // Ensure material is set to transparent mode
            SetMaterialTransparent();
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<LightChangeEvent>(gameObject, OnLightChangeEvent);
    }

    private void OnLightChangeEvent(LightChangeEvent evt)
    {
        OnInLightChange(evt.IsInLight);
    }

    private void SetMaterialTransparent()
    {
        // For URP/Lit shader
        if (_material.HasProperty("_Surface"))
        {
            _material.SetFloat("_Surface", 1); // Transparent
            _material.SetFloat("_Blend", 0); // Alpha blend
        }

        // Enable transparency
        _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _material.SetInt("_ZWrite", 0);
        _material.renderQueue = 3000;
    }

    public void OnInLightChange(bool isInLight)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _targetAlpha = isInLight ? 0f : 1f;
        _fadeCoroutine = StartCoroutine(FadeSequence(isInLight));
    }

    private IEnumerator FadeSequence(bool shouldFade)
    {
        // Wait for player to move if needed
        if (shouldFade && checkForPlayerOnTop)
        {
            yield return new WaitForSeconds(0.1f);
            while (IsPlayerOnTop())
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        float startAlpha = _currentAlpha;
        float elapsed = 0f;
        float duration = 1f / fadeSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / duration);
            _currentAlpha = Mathf.Lerp(startAlpha, _targetAlpha, t);

            UpdateMaterialAlpha(_currentAlpha);

            // Update physics based on alpha threshold
            if (shouldFade && _currentAlpha <= physicsDisableAlpha && _physicsEnabled)
            {
                DisablePhysics();
            }
            else if (!shouldFade && _currentAlpha > physicsDisableAlpha && !_physicsEnabled)
            {
                EnablePhysics();
            }

            yield return null;
        }

        _currentAlpha = _targetAlpha;
        UpdateMaterialAlpha(_currentAlpha);

        // Final physics state
        if (shouldFade)
        {
            DisablePhysics();
        }
        else
        {
            EnablePhysics();
        }
    }

    private void UpdateMaterialAlpha(float alpha)
    {
        if (_material == null) return;

        // Try different property names based on shader
        if (_material.HasProperty(BaseColorProperty))
        {
            Color color = _material.GetColor(BaseColorProperty);
            color.a = alpha;
            _material.SetColor(BaseColorProperty, color);
        }
        else if (_material.HasProperty(ColorProperty))
        {
            Color color = _material.GetColor(ColorProperty);
            color.a = alpha;
            _material.SetColor(ColorProperty, color);
        }

        // For sprite renderer
        if (m_SpriteRenderer != null)
        {
            Color spriteColor = m_SpriteRenderer.color;
            spriteColor.a = alpha;
            m_SpriteRenderer.color = spriteColor;
        }
    }

    private void DisablePhysics()
    {
        gameObject.layer = LayerMask.NameToLayer("Hidden");
        _physicsEnabled = false;
    }

    private void EnablePhysics()
    {
        gameObject.layer = _originalLayer;
        _physicsEnabled = true;
    }

    private bool IsPlayerOnTop()
    {
        if (_collider == null) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Player"));
        filter.useTriggers = false;

        var results = new Collider2D[5];
        int count = _collider.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            if (results[i].transform.position.y > transform.position.y + 0.1f)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        if (_material != null && Application.isPlaying)
        {
            Destroy(_material);
        }
    }
}
