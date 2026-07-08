using UnityEngine;

/// <summary>
/// Simpler alternative to SpriteBendToGround.
/// Casts a single ray downward and rotates the transform to align with the surface normal.
/// No mesh deformation — just rotation.
/// </summary>
public class SpriteAlignToGround : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("Layer mask for ground detection.")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [Tooltip("Height above the transform origin to start the raycast.")]
    [SerializeField] private float raycastHeight = 5f;
    [Tooltip("Maximum ray distance.")]
    [SerializeField] private float rayMaxDistance = 20f;
    [Tooltip("Small offset above the ground surface to prevent z-fighting.")]
    [SerializeField] private float surfaceOffset = 0.01f;

    [Header("Smoothing")]
    [Tooltip("How fast the rotation lerps to the target. Higher = snappier.")]
    [SerializeField] private float rotationSpeed = 10f;
    [Tooltip("Maximum rotation angle in degrees from upright (0 = no limit).")]
    [SerializeField] private float maxAngle = 0f;
    [Tooltip("Snap to ground position each frame.")]
    [SerializeField] private bool snapToGround = true;

    [Header("Update")]
    [SerializeField] private bool updateEveryFrame = true;

    private float _targetAngle;

    private void LateUpdate()
    {
        if (updateEveryFrame)
        {
            Refresh();
        }
    }

    /// <summary>
    /// Call this to manually refresh (useful when updateEveryFrame is false).
    /// </summary>
    public void Refresh()
    {
        Vector3 origin = transform.position + new Vector3(0f, raycastHeight, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayMaxDistance, groundLayer);

        if (hit.collider != null)
        {
            // Compute the Z rotation that aligns transform.up with the surface normal
            _targetAngle = Mathf.Atan2(hit.normal.x, hit.normal.y) * Mathf.Rad2Deg;

            if (maxAngle > 0f)
            {
                _targetAngle = Mathf.Clamp(_targetAngle, -maxAngle, maxAngle);
            }

            float currentAngle = transform.eulerAngles.z;
            // Wrap to [-180, 180] for smooth lerp
            float delta = Mathf.DeltaAngle(currentAngle, -_targetAngle);
            float newAngle = currentAngle + delta * Mathf.Clamp01(rotationSpeed * Time.deltaTime);
            transform.eulerAngles = new Vector3(0f, 0f, newAngle);

            if (snapToGround)
            {
                transform.position = new Vector3(transform.position.x, hit.point.y + surfaceOffset, transform.position.z);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + new Vector3(0f, raycastHeight, 0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, 0.05f);
        Gizmos.DrawLine(origin, origin + Vector3.down * rayMaxDistance);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayMaxDistance, groundLayer);
        if (hit.collider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hit.point, 0.08f);
            Gizmos.DrawLine((Vector3)hit.point, (Vector3)hit.point + (Vector3)hit.normal * 0.5f);
        }
    }
#endif
}
