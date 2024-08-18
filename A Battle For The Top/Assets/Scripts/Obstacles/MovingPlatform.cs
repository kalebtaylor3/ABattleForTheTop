using System.Collections;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform[] waypoints; // Array of transforms to define the path
    public float moveDuration = 2f; // Time it takes to move from one point to another
    public float waitTime = 1f; // Time to wait at each waypoint
    public bool loop = true; // Should the platform loop its movement
    public bool isPingPong = false; // Should the platform ping-pong back and forth

    private int currentWaypointIndex = 0; // Index of the current waypoint
    private bool movingForward = true; // Direction for movement
    private Transform playersOriginalParent;

    private void Start()
    {
        if (waypoints.Length > 1)
        {
            StartCoroutine(MovePlatform());
        }
    }

    private IEnumerator MovePlatform()
    {
        while (true)
        {
            // Get the next waypoint index based on the current direction
            int nextWaypointIndex = GetNextWaypointIndex();

            // Move the platform to the next waypoint
            while (Vector3.Distance(transform.position, waypoints[nextWaypointIndex].position) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, waypoints[nextWaypointIndex].position, (1 / moveDuration) * Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            // Update the current waypoint index after reaching the waypoint
            currentWaypointIndex = nextWaypointIndex;

            // Wait at the current waypoint, but not if ping-pong mode is active
            if (!isPingPong || (isPingPong && !IsTurningPoint()))
            {
                yield return new WaitForSeconds(waitTime);
            }

            // Determine the next direction or loop
            if (isPingPong || !loop)
            {
                if (currentWaypointIndex == waypoints.Length - 1 || currentWaypointIndex == 0)
                {
                    movingForward = !movingForward; // Reverse direction
                }
            }
            else if (loop && currentWaypointIndex == waypoints.Length - 1)
            {
                currentWaypointIndex = -1; // Prepare to start from the beginning
            }
        }
    }

    private int GetNextWaypointIndex()
    {
        if (movingForward)
        {
            return currentWaypointIndex + 1;
        }
        else
        {
            return currentWaypointIndex - 1;
        }
    }

    private bool IsTurningPoint()
    {
        return currentWaypointIndex == 0 || currentWaypointIndex == waypoints.Length - 1;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersOriginalParent = other.transform.parent;
            other.transform.SetParent(transform); // Make the player a child of the platform
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.SetParent(playersOriginalParent); // Remove the player from being a child of the platform
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            DrawArrow(waypoints[i].position, waypoints[i + 1].position);
        }

        if (loop && waypoints.Length > 1)
        {
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
            DrawArrow(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }
    }

    private void DrawArrow(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        Gizmos.DrawRay(to, right * 0.5f);
        Gizmos.DrawRay(to, left * 0.5f);
    }
}
