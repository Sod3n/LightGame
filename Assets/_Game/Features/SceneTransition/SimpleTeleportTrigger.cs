using UnityEngine;
using UnityEngine.Events;

namespace Light_and_controller.Scripts.Components
{
    public class SimpleTeleportTrigger : MonoBehaviour
    {
        [Header("Teleport Settings")]
        [SerializeField] private Transform destinationPoint;
        [SerializeField] private Vector2 destinationOffset = Vector2.zero;
        [SerializeField] private SimpleTeleportTrigger linkedTeleport;
        
        [Header("Options")]
        [SerializeField] private bool resetVelocity = true;
        [SerializeField] private bool preserveHorizontalVelocity = false;
        [SerializeField] private float teleportCooldown = 0.5f;
        [SerializeField] private float linkedTeleportDisableTime = 0.5f;
        
        [Header("Filter")]
        [SerializeField] private LayerMask teleportLayers = -1;
        [SerializeField] private string requiredTag = "Player";
        
        [Header("Events")]
        public UnityEvent onTeleport = new UnityEvent();
        
        private float _lastTeleportTime = -999f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Time.time - _lastTeleportTime < teleportCooldown)
                return;

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
                return;

            if (((1 << other.gameObject.layer) & teleportLayers) == 0)
                return;

            if (destinationPoint == null)
            {
                Debug.LogWarning($"SimpleTeleportTrigger on {gameObject.name}: No destination point assigned!");
                return;
            }

            TeleportObject(other.gameObject);
        }

        private void TeleportObject(GameObject obj)
        {
            Vector3 targetPosition = destinationPoint.position + (Vector3)destinationOffset;
            
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (resetVelocity)
                {
                    if (preserveHorizontalVelocity)
                    {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                    rb.angularVelocity = 0f;
                }
                
                rb.position = targetPosition;
            }
            else
            {
                obj.transform.position = targetPosition;
            }

            PlayerMain playerMain = obj.GetComponent<PlayerMain>();
            if (playerMain != null)
            {
                if (playerMain._stateMachine != null)
                {
                    playerMain._stateMachine.ChangeState(playerMain.IdleState);
                }
                
                if (playerMain.InputManager != null)
                {
                    playerMain.InputManager.ClearInput();
                }
            }

            _lastTeleportTime = Time.time;
            
            if (linkedTeleport != null)
            {
                linkedTeleport.DisableTemporarily(linkedTeleportDisableTime);
            }
            
            onTeleport.Invoke();
        }

        public void DisableTemporarily(float duration)
        {
            _lastTeleportTime = Time.time + duration - teleportCooldown;
        }

        private void OnDrawGizmos()
        {
            if (destinationPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, destinationPoint.position + (Vector3)destinationOffset);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(destinationPoint.position + (Vector3)destinationOffset, 0.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
        }
    }
}
