using System;
using System.Collections;
using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Transform _transform;

        public Vector2 Direction { get; set; }
        public float Speed { get; set; }
        public float SpeedRotation { get; set; }

        private void FixedUpdate()
        {
            Direction = (_transform.position - transform.position);

            rb.MovePosition(rb.position + Direction * (Speed * Time.fixedDeltaTime));
            rb.MoveRotation(rb.rotation + SpeedRotation * Time.fixedDeltaTime);
        }

        private IEnumerator OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger) yield break;
            yield return new WaitForFixedUpdate();
            Destroy(gameObject);
        }
    }
}