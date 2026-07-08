using UnityEngine;

/// <summary>
/// Component to attach to light objects to specify a teleport destination point.
/// The teleport destination can be set as a child transform or an offset from the light's position.
/// </summary>
public class TeleportDestination : MonoBehaviour
{
    [Tooltip("The specific point where the player should teleport to. If null, uses this object's position.")]
    public Transform DestinationPoint;

    [Tooltip("Offset from the destination point (or this object's position if DestinationPoint is null)")]
    public Vector2 Offset = Vector2.zero;

    /// <summary>
    /// Gets the world position where the player should teleport to.
    /// </summary>
    public Vector2 GetTeleportPosition()
    {
        if (DestinationPoint != null)
        {
            return (Vector2)DestinationPoint.position + Offset;
        }
        
        return (Vector2)transform.position + Offset;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the teleport destination in the editor
        Gizmos.color = Color.cyan;
        Vector2 destination = GetTeleportPosition();
        Gizmos.DrawWireSphere(destination, 0.5f);
        Gizmos.DrawLine(transform.position, destination);
    }
}
