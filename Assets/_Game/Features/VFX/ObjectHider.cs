using System.Collections.Generic;
using LightGame.Features;
using UnityEngine;

/// <summary>
/// Toggles this platform between solid and pass-through based on whether its
/// LightDetector currently sees light.
///
/// Drives the collider off the SAME per-frame signal (LightDetector.lightSprings)
/// that SimpleBlockToSpriteSync uses for the dissolve visual, so collision and
/// visuals can never disagree. Previously this reacted to an EventBus edge event,
/// which could miss transitions while the dissolve (polling) kept working —
/// leaving dissolved platforms still solid.
/// </summary>
[RequireComponent(typeof(LightDetector))]
public class ObjectHider : MonoBehaviour
{
    [SerializeField] private List<Renderer> m_Renderer;

    private LightDetector _detector;
    private int _originalLayer;
    private int _hiddenLayer;
    private bool _hidden;

    private void Awake()
    {
        _detector = GetComponent<LightDetector>();
        _originalLayer = gameObject.layer;
        _hiddenLayer = LayerMask.NameToLayer("Hidden");
    }

    private void Update()
    {
        bool inLight = _detector != null && _detector.lightSprings.Count > 0;
        if (inLight == _hidden) return;
        if (inLight) HideCollider();
        else ShowCollider();
    }

    void HideCollider()
    {
        _hidden = true;
        gameObject.layer = _hiddenLayer;
        m_Renderer.ForEach(x => { if (x != null) x.enabled = false; });
    }

    public void ShowCollider()
    {
        _hidden = false;
        gameObject.layer = _originalLayer;
        m_Renderer.ForEach(x => { if (x != null) x.enabled = true; });
    }

    public void OnInLightChange(bool isInLight)
    {
        if (isInLight) HideCollider();
        else ShowCollider();
    }
}
