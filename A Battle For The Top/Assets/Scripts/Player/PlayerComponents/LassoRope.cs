using System.Collections;
using UnityEngine;

public class LassoRope : MonoBehaviour
{
    [HideInInspector] public LineRenderer lineRenderer;
    public int quality = 20;  // Increase quality for smoother curves
    [HideInInspector] public Spring spring;
    public float damper = 5f;
    public float strenght = 100f;
    public float velocity = 20f;
    public float waveCount = 5f; // Increase wave count for more loops
    public float waveHeight = 0.2f; // Increase wave height for more pronounced loops
    public AnimationCurve affectCurve;
    public Color _color = Color.yellow;  // Typical lasso color
    public float startWidth;
    public float endWidth;
    private float initialWaveHeight; // To store the initial wave height
    private bool isRetracting = false; // Track whether the rope is retracting
    private float retractProgress = 0f; // Track the progress of retraction
    public float retractTime = 0f;

    private void Awake()
    {
        // Initialize the LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = startWidth; // Thicker rope
        lineRenderer.endWidth = endWidth;  // Slightly taper the end
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Ensure you have a material
        lineRenderer.startColor = _color;
        lineRenderer.endColor = _color;
        lineRenderer.enabled = false; // Initially disable the LineRenderer
        spring = new Spring();
        spring.SetTarget(0);

        // Store the initial wave height for resetting during retraction
        initialWaveHeight = waveHeight;

        // Default affectCurve to make the lasso look more dynamic
        if (affectCurve == null || affectCurve.keys.Length == 0)
        {
            affectCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
        }
    }

    public void UpdateLineRenderer(Transform grappleSpawn, Vector3 grapplePoint)
    {
        if (lineRenderer.enabled)
        {
            if (lineRenderer.positionCount == 0)
            {
                spring.SetVelocity(velocity);
                lineRenderer.positionCount = quality + 1;
            }

            spring.SetDamper(damper);
            spring.SetStrength(strenght);
            spring.Update(Time.deltaTime);

            var up = Quaternion.LookRotation(grapplePoint - grappleSpawn.position).normalized * Vector3.up;

            // Reverse the wave pattern if retracting
            float currentWaveCount = isRetracting ? waveCount * (1 - retractProgress) : waveCount;
            float currentWaveHeight = isRetracting ? Mathf.Lerp(initialWaveHeight, 0f, retractProgress) : waveHeight;

            for (int i = 0; i < quality + 1; i++)
            {
                var delta = i / (float)quality;

                // Invert the sine wave for retraction by adjusting the phase
                float sineWave = Mathf.Sin((delta * currentWaveCount * Mathf.PI) + (isRetracting ? Mathf.PI : 0));

                // Apply sine wave offset with reverse effect during retraction
                var offset = up * currentWaveHeight * sineWave * affectCurve.Evaluate(delta);

                lineRenderer.SetPosition(i, Vector3.Lerp(grappleSpawn.position, grapplePoint, delta) + offset);
            }
        }
    }

    public void EnableLasso()
    {
        isRetracting = false; // Set the flag to false when the lasso is extended
        lineRenderer.enabled = true;
    }

    public void DisableLasso()
    {
        lineRenderer.enabled = false;
    }

    public void StartRetracting()
    {
        isRetracting = true; // Set the flag to true when retraction starts
        StartCoroutine(RetractOverTime(retractTime));
    }

    private IEnumerator RetractOverTime(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            retractProgress = elapsedTime / duration; // Calculate retraction progress
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        retractProgress = 1f; // Fully retracted
    }
}
