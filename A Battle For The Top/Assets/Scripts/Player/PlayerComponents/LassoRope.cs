using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LassoRope : MonoBehaviour
{
    [HideInInspector] public LineRenderer lineRenderer;
    public int segments = 20; // Number of segments in the lasso loop
    public float lassoRadius = 2f; // Initial radius of the lasso loop
    public float rotationSpeed = 5f; // Speed at which the lasso rotates
    public float lassoTightness = 1f; // Controls how tightly the lasso loop is wound
    public AnimationCurve radiusCurve; // Curve to control the radius change during spinning

    private float currentRotation = 0f;

    private void Awake()
    {
        // Initialize the LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = segments + 1; // Closed loop
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.enabled = false; // Initially disable the LineRenderer
    }

    public void UpdateLassoRope(Transform lassoPivot, Vector3 targetPoint)
    {
        if (lineRenderer.enabled)
        {
            // Adjust the lasso radius based on the distance to the target
            float distance = Vector3.Distance(lassoPivot.position, targetPoint);
            float adjustedRadius = lassoRadius * radiusCurve.Evaluate(distance / lassoTightness);

            // Increment rotation angle
            currentRotation += rotationSpeed * Time.deltaTime;

            // Update the lasso positions in a circular pattern
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments + currentRotation;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * adjustedRadius;

                lineRenderer.SetPosition(i, lassoPivot.position + offset);
            }
        }
    }

    public void EnableLasso()
    {
        lineRenderer.enabled = true;
    }

    public void DisableLasso()
    {
        lineRenderer.enabled = false;
    }
}
