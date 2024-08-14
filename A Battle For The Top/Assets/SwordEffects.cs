using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordEffects : MonoBehaviour
{
    public bool activated = false;
    public float rotationSpeed;
    public bool hitSomething = false;

    // Desired rotation offset when the sword lands
    public Vector3 rotationOffset = new Vector3(0, 0, 0);  // Adjust this based on your sword's model orientation

    // Position offset to pull the sword back from the wall
    public float positionOffset = 0.1f;  // Adjust this to control how far back the sword is pulled after hitting

    private BoxCollider boxCollider;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (activated)
            transform.localEulerAngles += transform.right * rotationSpeed * Time.unscaledDeltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (activated)
        {
            activated = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            hitSomething = true;

            // Get the normal of the surface where the sword collided
            Vector3 surfaceNormal = collision.contacts[0].normal;

            // Align the sword's forward direction with the surface normal
            Quaternion targetRotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);

            // Apply the rotation offset if needed
            targetRotation *= Quaternion.Euler(rotationOffset);

            // Set the sword's rotation to the calculated target rotation
            transform.rotation = targetRotation;

            // Apply position offset to prevent the sword from being stuck in the wall
            transform.position = collision.contacts[0].point - transform.forward * positionOffset;

            //is in the wall so get rid of exclude layer for box collider and rigid body.
            rb.excludeLayers = 0;
            boxCollider.excludeLayers = 0;
        }
    }
}
