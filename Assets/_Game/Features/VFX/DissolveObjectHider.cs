using System.Collections;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

[RequireComponent(typeof(LightDetector))]
public class DissolveObjectHider : MonoBehaviour, ILightable
{
    [Header("References")]
    [SerializeField] private MeshRenderer m_Renderer;

    [Header("Dissolve Settings")]
    [SerializeField] private float dissolveSpeed = 2f;
    [SerializeField] private float physicsDisableThreshold = 0.5f; // When to disable physics (0-1)
    [SerializeField] private bool smoothTransition = true;

    [Header("Physics Settings")]
    [SerializeField] private bool checkForPlayerOnTop = true;
    [SerializeField] private float playerCheckDelay = 0.1f;

    private Material _dissolveMaterial;
    private int _originalLayer;
    private float _currentDissolveAmount = 0f;
    private bool _isDissolving = false;
    private Coroutine _dissolveCoroutine;
    private Collider2D _collider;

    private static readonly int DissolveAmountProperty = Shader.PropertyToID("_DissolveAmount");

    private void Start()
    {
        _originalLayer = gameObject.layer;
        _collider = GetComponent<Collider2D>();

        // Create instance of material to avoid affecting other objects
        if (m_Renderer != null)
        {
            _dissolveMaterial = m_Renderer.material;
            _dissolveMaterial.SetFloat(DissolveAmountProperty, 0f);
        }
        else
        {
            Debug.LogWarning($"MeshRenderer not assigned on {gameObject.name}");
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

    public void OnInLightChange(bool isInLight)
    {
        if (_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
        }

        if (isInLight)
        {
            _dissolveCoroutine = StartCoroutine(DissolveSequence(true));
        }
        else
        {
            _dissolveCoroutine = StartCoroutine(DissolveSequence(false));
        }
    }

    private IEnumerator DissolveSequence(bool shouldDissolve)
    {
        float targetAmount = shouldDissolve ? 1f : 0f;

        // If dissolving and we should check for player
        if (shouldDissolve && checkForPlayerOnTop)
        {
            // Wait a bit before checking
            yield return new WaitForSeconds(playerCheckDelay);

            // Wait until player is not on top
            while (IsPlayerOnTop())
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        _isDissolving = true;

        // Animate dissolve
        while (Mathf.Abs(_currentDissolveAmount - targetAmount) > 0.01f)
        {
            _currentDissolveAmount = Mathf.MoveTowards(
                _currentDissolveAmount,
                targetAmount,
                dissolveSpeed * Time.deltaTime
            );

            // Update shader
            if (_dissolveMaterial != null)
            {
                _dissolveMaterial.SetFloat(DissolveAmountProperty, _currentDissolveAmount);
            }

            // Disable physics when dissolve reaches threshold
            if (shouldDissolve && _currentDissolveAmount >= physicsDisableThreshold)
            {
                DisablePhysics();
            }
            else if (!shouldDissolve && _currentDissolveAmount < physicsDisableThreshold)
            {
                EnablePhysics();
            }

            yield return null;
        }

        // Finalize
        _currentDissolveAmount = targetAmount;
        if (_dissolveMaterial != null)
        {
            _dissolveMaterial.SetFloat(DissolveAmountProperty, _currentDissolveAmount);
        }

        if (shouldDissolve)
        {
            DisablePhysics();
        }
        else
        {
            EnablePhysics();
        }

        _isDissolving = false;
    }

    private void DisablePhysics()
    {
        gameObject.layer = LayerMask.NameToLayer("Hidden");
    }

    private void EnablePhysics()
    {
        gameObject.layer = _originalLayer;
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
            // Check if player is above this object (with small tolerance)
            if (results[i].transform.position.y > transform.position.y + 0.1f)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (_dissolveMaterial != null && Application.isPlaying)
        {
            Destroy(_dissolveMaterial);
        }
    }

    // Public methods for manual control
    public void SetDissolveAmount(float amount)
    {
        _currentDissolveAmount = Mathf.Clamp01(amount);
        if (_dissolveMaterial != null)
        {
            _dissolveMaterial.SetFloat(DissolveAmountProperty, _currentDissolveAmount);
        }
    }

    public float GetDissolveAmount()
    {
        return _currentDissolveAmount;
    }

    public bool IsCurrentlyDissolving()
    {
        return _isDissolving;
    }
}
