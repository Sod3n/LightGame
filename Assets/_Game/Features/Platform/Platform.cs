using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float speed = 2f;
    [SerializeField] private float waypointThreshold = 0.1f;
    
    private Rigidbody2D rb;
    private int currentWaypointIndex = 0;
    private int direction = 1; // 1 for forward, -1 for backward

    void Awake()
    {
        // Move waypoints to platform's parent
        Transform parentTransform = transform.parent;
        if (parentTransform != null)
        {
            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null && waypoint.parent == transform)
                {
                    waypoint.SetParent(parentTransform);
                }
            }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        PlatformMovement();
    }

    private void PlatformMovement()
    {
        if (waypoints.Count < 2)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetWaypoint = waypoints[currentWaypointIndex].position;
        Vector2 currentPosition = transform.position;
        
        // Calculate direction to target waypoint
        Vector2 directionToTarget = (targetWaypoint - currentPosition).normalized;
        
        // Check if we've reached the current waypoint
        if (Vector2.Distance(currentPosition, targetWaypoint) <= waypointThreshold)
        {
            // Move to next waypoint
            currentWaypointIndex += direction;
            
            // Check if we need to reverse direction
            if (currentWaypointIndex >= waypoints.Count)
            {
                currentWaypointIndex = waypoints.Count - 2;
                direction = -1;
            }
            else if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = 1;
                direction = 1;
            }
        }
        
        // Move towards the target waypoint
        rb.linearVelocity = directionToTarget * speed;
    }
}