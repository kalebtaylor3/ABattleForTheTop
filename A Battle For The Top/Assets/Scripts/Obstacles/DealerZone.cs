using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Abilities;
using BFTT.Controller;

[ExecuteInEditMode]
public class DealerZone : MonoBehaviour
{
    public Color zoneColor = new Color(1, 0, 0, 0.5f); // Semi-transparent red
    public GameObject dealerCamera;
    private Vector3 originalScale;
    bool InZone = false;

    private void Awake()
    {
        dealerCamera.SetActive(false);
    }

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

    private void OnTriggerEnter(Collider other)
    {
        //scale up the player.
        //set speed
        //turn off player camera
        //turn on dealer camera

        if(other.gameObject.tag == "Player" && !InZone)
        {
            originalScale = other.transform.localScale;
            other.transform.localScale = other.transform.localScale * 5;
            other.GetComponent<Locomotion>().SetSpeeds(14, 17);
            other.GetComponent<PlayerController>().CinemachineCameraTarget.SetActive(false);
            dealerCamera.SetActive(true);
            InZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            other.transform.localScale = originalScale;
            other.GetComponent<Locomotion>().SetSpeeds(4, 7);
            other.GetComponent<PlayerController>().CinemachineCameraTarget.SetActive(true);
            dealerCamera.SetActive(false);
            InZone = false;
        }
    }
}

