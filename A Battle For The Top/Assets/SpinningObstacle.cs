using BFTT.Components;
using BFTT.Controller;
using System;
using System.Collections;
using UnityEngine;

public class SpinningObstacle : MonoBehaviour
{
    public float launchForce = 10f; // Adjust this value to control the launch force
    public float rayDistance = 10f; // Maximum distance for the raycast

    public static event Action OnCollision;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            RigidbodyMover playerMover = collision.gameObject.GetComponent<RigidbodyMover>();
            collision.gameObject.GetComponent<PlayerController>().Move = Vector2.zero;
            if (playerMover != null)
            {
                Transform playerTransform = collision.transform;
                Vector3 toObstacle = (transform.position - playerTransform.position).normalized;
                Vector3 playerForward = playerTransform.forward;

                // Check if the player is looking at the obstacle
                if (Vector3.Dot(playerForward, toObstacle) > 0)
                {
                    // Launch the player in the opposite direction they are looking
                    Vector3 launchDirection = playerForward;
                    launchDirection *= launchForce;
                    collision.gameObject.GetComponent<Rigidbody>().AddForceAtPosition(launchDirection + Vector3.up * 5, transform.position, ForceMode.Impulse);
                    this.GetComponent<BoxCollider>().enabled = false;
                    Debug.Log("bump - launched opposite direction");
                    OnCollision?.Invoke();
                }
                else
                {
                    // Perform a raycast from the obstacle to the player
                    Ray ray = new Ray(transform.position, (collision.transform.position - transform.position).normalized);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, rayDistance))
                    {
                        if (hit.collider.CompareTag("Player"))
                        {
                            // Calculate the launch direction away from the obstacle
                            Vector3 launchDirection = (hit.point - transform.position).normalized;
                            launchDirection *= launchForce;

                            // Apply force at the point of the collision
                            collision.gameObject.GetComponent<Rigidbody>().AddForceAtPosition(launchDirection + Vector3.up * 5, hit.point, ForceMode.Impulse);
                            Debug.Log("bump - launched from collision point");
                            OnCollision?.Invoke();
                        }
                    }
                }
            }
        }
    }
}
