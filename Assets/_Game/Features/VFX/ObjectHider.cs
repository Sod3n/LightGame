using System.Collections.Generic;
using System.Linq;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Systems;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(LightDetector))]
public class ObjectHider : MonoBehaviour, ILightable
{
    [SerializeField] private List<Renderer> m_Renderer;

    private int _originalLayer;
    
    private void Start()
    {
        _originalLayer = gameObject.layer;
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

    void HideCollider()
    {
        gameObject.layer = LayerMask.NameToLayer("Hidden");
        m_Renderer.ForEach(x => x.enabled = false);
    }


    public void ShowCollider()
    {
        gameObject.layer = _originalLayer;
        m_Renderer.ForEach(x => x.enabled = true);
    }

    public void OnInLightChange(bool isInLight)
    {
        if(isInLight) HideCollider();
        else ShowCollider();
    }
}
