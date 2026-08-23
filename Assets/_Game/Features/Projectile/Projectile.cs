using System;
using UnityEngine;

namespace LightGame.Features
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger) return;
            Destroy(gameObject);
        }
    }
}