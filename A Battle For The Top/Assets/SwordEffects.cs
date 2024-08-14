using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SwordEffects : MonoBehaviour
{
    public bool activated = false;
    public float rotationSpeed;
    public bool hitSomething = false;

    public Vector3 rotationOffset = new Vector3(0, 0, 0);
    public float positionOffset = 0.1f;

    private MeshCollider meshCollider;
    private BoxCollider boxColliderTrigger;
    private Rigidbody rb;

    public float bendStrength = 1f;
    public int hingePoints = 5;
    public AnimationCurve bendingCurve;
    public Vector3 bendDirection = Vector3.down; // Change to down for the handle to bend downwards

    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private Mesh mesh;

    private void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        boxColliderTrigger = GetComponent<BoxCollider>();
        meshCollider.convex = true; // Set to true if interacting with rigidbody
        rb = GetComponent<Rigidbody>();

        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        modifiedVertices = new Vector3[originalVertices.Length];

        if (bendingCurve == null)
        {
            bendingCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        }
    }

    private void Update()
    {
        if (activated)
        {
            transform.localEulerAngles += transform.right * rotationSpeed * Time.unscaledDeltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (activated)
        {
            activated = false;
            rb.isKinematic = true;
            hitSomething = true;

            Vector3 surfaceNormal = collision.contacts[0].normal;
            Quaternion targetRotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);
            targetRotation *= Quaternion.Euler(rotationOffset);
            transform.rotation = targetRotation;

            transform.position = collision.contacts[0].point - transform.forward * positionOffset;

            rb.excludeLayers = 0;
            //boxCollider.excludeLayers = 0;
            meshCollider.excludeLayers = 0;
            boxColliderTrigger.excludeLayers = 0;
        }
    }

    public void ApplyBend(float weight)
    {
        float boardLength = GetBoardLength();
        float hingeSpacing = boardLength / hingePoints;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];
            Vector3 cumulativeBend = Vector3.zero;

            // Ensure that vertices closer to the handle bend more than those near the tip
            for (int hinge = 1; hinge <= hingePoints; hinge++)
            {
                float hingePosition = hinge * hingeSpacing;
                if (vertex.z < hingePosition)  // Changed condition to bend vertices closer to the handle
                {
                    float relativePosition = (hingePosition - vertex.z) / boardLength; // Reverse the relative position calculation
                    float bendAmount = bendingCurve.Evaluate(relativePosition) * bendStrength * weight / hingePoints;
                    cumulativeBend += bendDirection * bendAmount;
                }
            }

            vertex += cumulativeBend;
            modifiedVertices[i] = vertex;
        }

        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        UpdateCollider();
    }

    private void UpdateCollider()
    {
        // Update the mesh collider to match the modified mesh
        meshCollider.sharedMesh = null; // Reset the collider
        meshCollider.sharedMesh = mesh; // Reassign the updated mesh
    }

    float GetBoardLength()
    {
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (Vector3 vertex in originalVertices)
        {
            if (vertex.z < minZ) minZ = vertex.z;
            if (vertex.z > maxZ) maxZ = vertex.z;
        }

        return maxZ - minZ;
    }
}
