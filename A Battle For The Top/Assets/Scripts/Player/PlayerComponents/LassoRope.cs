using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LassoRope : MonoBehaviour
{
    [HideInInspector] public LineRenderer lineRenderer;
    public int quality;
    [HideInInspector] public Spring spring;
    public float damper;
    public float strenght;
    public float velocity;
    public float waveCount;
    public float waveHeight;
    public AnimationCurve affectCurve;


    private void Awake()
    {
        // Initialize the LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Ensure you have a material
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.enabled = false; // Initially disable the LineRenderer
        spring = new Spring();
        spring.SetTarget(0);
    }

    public void UpdateLineRenderer(Transform grappleSpawn, Vector3 grapplePoint)
    {
        if (lineRenderer.enabled)
        {
            //lineRenderer.SetPosition(0, grappleSpawn.position); // Start point of the rope
            //lineRenderer.SetPosition(1, grapplePoint); // End point of the rope
            if (lineRenderer.positionCount == 0)
            {
                spring.SetVelocity(velocity);
                lineRenderer.positionCount = quality + 1;
            }

            spring.SetDamper(damper);
            spring.SetStrength(strenght);
            spring.Update(Time.deltaTime);
            var up = Quaternion.LookRotation(grapplePoint - grappleSpawn.position).normalized * Vector3.up;


            for (int i = 0; i < quality + 1; i++)
            {
                var delta = i / (float)quality;
                var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

                lineRenderer.SetPosition(i, Vector3.Lerp(grappleSpawn.position, grapplePoint, delta) + offset);
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
