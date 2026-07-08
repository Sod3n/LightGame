using System.Collections;
using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy
{
    /// <summary>
    /// Enemy projectile with cannon-like physics (strong gravity effect)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float gravityScale = 2f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float rotationLerpSpeed = 5f;
        [SerializeField] private float rotationOffset = 0f;
        
        private Vector2 initialVelocity;
        
        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }
        }
        
        private void Start()
        {
            // Set gravity scale for cannon-like arc
            rb.gravityScale = gravityScale;
            
            // Destroy after lifetime
            Destroy(gameObject, lifetime);
        }
        
        public void Initialize(Vector2 direction, float speed, float rotSpeed = 0f)
        {
            initialVelocity = direction.normalized * speed;
            rb.linearVelocity = initialVelocity;
            
            // Set initial rotation to match velocity direction with offset
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        private void FixedUpdate()
        {
            // Rotate to match velocity direction with offset
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                float targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg + rotationOffset;
                float currentAngle = rb.rotation;
                float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationLerpSpeed * Time.fixedDeltaTime);
                rb.MoveRotation(newAngle);
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Don't destroy on trigger colliders
            if (other.isTrigger) return;
            
            // Destroy on contact with solid objects
            Destroy(gameObject);
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Destroy on collision
            Destroy(gameObject);
        }
    }
}
