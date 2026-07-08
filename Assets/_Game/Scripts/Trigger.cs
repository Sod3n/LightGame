using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts;

public class Trigger : MonoBehaviour
{
    [SerializeField] private Transform targetLightPoint;
    [SerializeField] private Collider2D collider2D;
    [SerializeField] private Rigidbody2D lightRigidbody;
    [SerializeField] private LightType lightType = LightType.Default;

    [Header("Level Change Settings (only for LevelChange light type)")]
    [SerializeField] private bool useNextScene = true;
    [SerializeField] private SceneName targetScene;

    public LightType LightType => lightType;
    public bool UseNextScene => useNextScene;
    public SceneName TargetScene => targetScene;
    public Transform TargetLightPoint => targetLightPoint;
    public Collider2D LightCollider => collider2D;

    private async void Start()
    {
        await UniTask.WaitForFixedUpdate();
        // Check for any colliders already overlapping at start
        var contacts = new List<Collider2D>();
        collider2D.GetContacts(contacts);

        foreach (var contact in contacts)
        {
            if (contact != null)
            {
                Handle(contact);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Handle(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Handle(other); 
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if(!other.TryGetComponent<LightDetector>(out var component)) return;
        var contacts = new List<Collider2D>();
        collider2D.GetContacts(contacts);
        if(contacts.Any(x => x.gameObject == other.gameObject)) return;

        component.RemoveLightSource(gameObject, lightType);
    }

    private void Handle(Collider2D other)
    {
        if(!other.TryGetComponent<LightDetector>(out var component)) return;
        
        var is_lighted = component.LightBlockCheck(targetLightPoint.position, lightRigidbody);
//        Debug.Log(is_lighted);
        if (is_lighted) component.AddLightSource(gameObject, lightType);
        else component.RemoveLightSource(gameObject, lightType);
    }
}
