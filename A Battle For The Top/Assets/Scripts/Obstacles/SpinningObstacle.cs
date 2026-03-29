using BFTT.Components;
using BFTT.Controller;
using System;
using System.Collections;
using UnityEngine;

public class SpinningObstacle : MonoBehaviour
{
    public float launchForce = 10f; // Adjust this value to control the launch force
    public float upwardLaunchForce = 5f;
    [SerializeField] private float outwardLaunchWeight = 1.25f;
    [Tooltip("Optional explicit pivot. If left empty, the parent transform is used.")]
    [SerializeField] private Transform spinCenter;
    [Tooltip("Treat the spinner as rotating clockwise around its up axis.")]
    [SerializeField] private bool clockwise = true;

    public static event Action OnCollision;

    private void Awake()
    {
        if (spinCenter == null)
            spinCenter = transform.parent;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            RigidbodyMover playerMover = collision.gameObject.GetComponent<RigidbodyMover>();
            collision.gameObject.GetComponent<PlayerController>().Move = Vector2.zero;
            if (playerMover != null)
            {
                Rigidbody playerRigidbody = collision.gameObject.GetComponent<Rigidbody>();
                Vector3 pivotPosition = spinCenter != null ? spinCenter.position : transform.position;
                Vector3 spinAxis = spinCenter != null ? spinCenter.up : transform.up;
                ContactPoint contact = collision.GetContact(0);
                Vector3 radialDirection = contact.point - pivotPosition;
                radialDirection = Vector3.ProjectOnPlane(radialDirection, spinAxis);

                if (radialDirection.sqrMagnitude < 0.001f)
                    radialDirection = Vector3.ProjectOnPlane(playerRigidbody.worldCenterOfMass - pivotPosition, spinAxis);

                radialDirection.Normalize();

                // Tangential sweep direction at the hit point.
                Vector3 tangentialDirection = clockwise
                    ? Vector3.Cross(radialDirection, spinAxis.normalized)
                    : Vector3.Cross(spinAxis.normalized, radialDirection);
                tangentialDirection.Normalize();

                // Blend spin direction with an outward push so hits always throw the player away
                // from the wheel instead of letting their own momentum carry them over the paddle.
                Vector3 launchDirection = (tangentialDirection + radialDirection * outwardLaunchWeight).normalized;

                playerRigidbody.linearVelocity = Vector3.zero;

                playerRigidbody.AddForce(launchDirection * launchForce + Vector3.up * upwardLaunchForce, ForceMode.VelocityChange);
                GetComponent<BoxCollider>().enabled = false;
                Debug.Log("bump - launched in spin direction");
                OnCollision?.Invoke();
            }
        }
    }
}
