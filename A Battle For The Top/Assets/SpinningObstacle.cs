using BFTT.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinningObstacle : MonoBehaviour
{
    public float launchForce = 10f; // Adjust this value to control the launch force

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            RigidbodyMover playerMover = collision.gameObject.GetComponent<RigidbodyMover>();
            if (playerMover != null && playerMover.Grounded)
            {
                // Perform a raycast from the obstacle to the player
                Vector3 directionToPlayer = collision.transform.position - transform.position;
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToPlayer, out hit))
                {
                    if (hit.collider.gameObject == collision.gameObject)
                    {
                        // Calculate the launch direction
                        Vector3 launchDirection = directionToPlayer.normalized;

                        // Set the player's velocity
                        playerMover.SetVelocity(launchDirection * launchForce);
                        collision.gameObject.GetComponent<Rigidbody>().AddForce(launchDirection * launchForce + Vector3.up * 3f, ForceMode.Impulse);

                        Debug.Log("bump");
                    }
                }
            }
        }
    }
}
