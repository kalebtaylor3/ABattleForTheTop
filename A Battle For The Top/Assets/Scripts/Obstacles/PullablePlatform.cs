using System.Collections;
using UnityEngine;

public class PullablePlatform : MonoBehaviour
{
    public Transform pointA; // Starting position of the platform
    public Transform pointB; // Target position of the platform
    public float pullSpeed = 2f; // Speed at which the platform moves
    public Color gizmoColor = Color.green; // Color for gizmos
    public float gizmoSize = 0.5f; // Size of gizmo spheres
    public float arrowHeadLength = 0.3f; // Length of arrowhead for direction indicators
    public float arrowHeadAngle = 20f; // Angle of arrowhead

    private bool isPulling = false; // Tracks whether the platform is currently pulling
    private Vector3 targetPosition;

    void Start()
    {
        transform.position = pointA.position; // Set initial position
    }

    void Update()
    {
        if (isPulling)
        {
            // Smoothly move the platform towards point B
            transform.position = Vector3.Lerp(transform.position, targetPosition, pullSpeed * Time.deltaTime);

            // Check if the platform has reached the target
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isPulling = false; // Stop pulling when close enough
            }
        }
    }

    // Public method to start pulling the platform
    public void Pull(Transform pullPoint)
    {
        isPulling = true;
        targetPosition = pullPoint.position;
    }

    // Draw detailed gizmos to visually represent the platform's path and state
    void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            // Draw a line between point A and point B
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(pointA.position, pointB.position);

            // Draw spheres at point A and point B to visualize the start and target
            Gizmos.DrawSphere(pointA.position, gizmoSize);
            Gizmos.DrawSphere(pointB.position, gizmoSize);

            // Draw an arrow to show the current direction of movement (if pulling)
            if (isPulling)
            {
                DrawArrow(transform.position, targetPosition - transform.position);
            }

            // Label point A and point B
            Gizmos.color = Color.white;
            GizmosUtils.DrawLabel(pointA.position, "Point A (Start)");
            GizmosUtils.DrawLabel(pointB.position, "Point B (Target)");

            // Display the platform's current position and movement progress
            GizmosUtils.DrawLabel(transform.position, $"Platform Position: {transform.position}");
            if (isPulling)
            {
                float distanceCovered = Vector3.Distance(pointA.position, transform.position);
                float totalDistance = Vector3.Distance(pointA.position, pointB.position);
                float progress = (distanceCovered / totalDistance) * 100f;
                GizmosUtils.DrawLabel(transform.position + Vector3.up * 0.5f, $"Progress: {progress:F1}%");
            }
        }
    }

    // Draw an arrow to represent movement direction
    private void DrawArrow(Vector3 position, Vector3 direction)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(position, direction);

        // Draw arrowhead
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;
        Gizmos.DrawRay(position + direction, right * arrowHeadLength);
        Gizmos.DrawRay(position + direction, left * arrowHeadLength);
    }
}

// Utility class to help with custom gizmo drawing like labels
public static class GizmosUtils
{
    public static void DrawLabel(Vector3 position, string text)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(position, text);
#endif
    }
}
