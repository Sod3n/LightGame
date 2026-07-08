using System;
using System.Collections.Generic;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private bool useCollision = false;
    
    private HashSet<GameObject> ObjectsInTrigger { get; } = new();
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(useCollision) return;
        if(!ObjectsInTrigger.Add(other.gameObject)) return;
        EventBus.Publish(other.gameObject, new DamageEvent { Amount = damage });
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(useCollision) return;
        ObjectsInTrigger.Remove(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(!useCollision) return;
        if(!ObjectsInTrigger.Add(collision.gameObject)) return;
        EventBus.Publish(collision.gameObject, new DamageEvent { Amount = damage });
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if(!useCollision) return;
        ObjectsInTrigger.Remove(collision.gameObject);
    }
}
