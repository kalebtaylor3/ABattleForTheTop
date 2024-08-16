using UnityEngine;

[ExecuteInEditMode]
public class CombatZone : MonoBehaviour
{
    public Color zoneColor = new Color(1, 0, 0, 0.5f); // Semi-transparent red

    private void OnDrawGizmos()
    {
        // Set the Gizmo color
        Gizmos.color = zoneColor;

        // Draw a wire cube at the position and size of the zone
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        // Optionally, draw a solid cube if you want it filled
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.25f); // Make it more transparent
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
