using System;
using System.Collections.Generic;
using LightGame.Features;
using LightGame.Globals;
using UnityEngine;

using LightGame.Core;
public class DamageTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("Damage dealt on contact (HP points).")]
    private int damage;

    [SerializeField, Tooltip("If ON: fires on physical collision (Collider must NOT be a trigger).\nIf OFF: fires on trigger overlap (Collider must be a trigger).")]
    private bool useCollision = false;
    
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
