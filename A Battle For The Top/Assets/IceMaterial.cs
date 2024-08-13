using System.Collections;
using UnityEngine;

public class IceMaterial : MonoBehaviour
{
    public Shader customShader;  // Reference to the shader

    [HideInInspector] public Material iceMaterial;
    public float meltDuration = 5f;  // Duration for the melting process
    public float freezeDuration = 5f;  // Duration for the refreezing process
    private Coroutine meltCoroutine;
    private Coroutine freezeCoroutine;

    public BoxCollider collider;

    private bool isFullyMelted = false;  // Track if the ice is fully melted

    void Start()
    {
        // Create a new material instance using the custom shader
        iceMaterial = new Material(customShader);

        // Assign the unique material to the object's renderer
        GetComponent<Renderer>().material = iceMaterial;
        iceMaterial.SetFloat("_Dissolve", 0f); // Ensure the ice starts fully intact
    }

    public void StartMelting()
    {
        if (isFullyMelted) return;  // If fully melted, do nothing

        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);  // Interrupt refreezing
            freezeCoroutine = null;
        }

        if (meltCoroutine == null)
        {
            meltCoroutine = StartCoroutine(MeltIce());
        }
    }

    public void StopMelting()
    {
        if (isFullyMelted) return;  // If fully melted, do nothing

        if (meltCoroutine != null)
        {
            StopCoroutine(meltCoroutine);  // Interrupt melting
            meltCoroutine = null;
        }

        if (freezeCoroutine == null)
        {
            freezeCoroutine = StartCoroutine(RefreezeIce());
        }
    }

    private IEnumerator MeltIce()
    {
        float initialDissolve = iceMaterial.GetFloat("_Dissolve");
        float dissolveRate = (1f - initialDissolve) / meltDuration;

        while (initialDissolve < 1f)
        {
            initialDissolve += dissolveRate * Time.deltaTime;
            iceMaterial.SetFloat("_Dissolve", Mathf.Clamp(initialDissolve, 0f, 1f));

            // Failsafe: If the dissolve value is very close to 1, smoothly transition it to 1
            if (initialDissolve >= 0.5f)
            {
                float smoothDuration = 1f;  // The time it takes to smoothly transition to fully melted
                float smoothElapsedTime = 0f;

                isFullyMelted = true;  // Mark as fully melted, preventing refreezing

                while (smoothElapsedTime < smoothDuration)
                {
                    smoothElapsedTime += Time.deltaTime;
                    float smoothDissolve = Mathf.Lerp(initialDissolve, 1f, smoothElapsedTime / smoothDuration);
                    iceMaterial.SetFloat("_Dissolve", smoothDissolve);
                    yield return null;
                }

                // Ensure the dissolve value is exactly 1 after the smooth transition
                iceMaterial.SetFloat("_Dissolve", 1f);
                meltCoroutine = null;  // Reset the coroutine reference
                collider.enabled = false;
                Debug.Log("ice is melted you wont slide now");
                yield break;
            }

            yield return null;
        }

        // Ensure the dissolve value is exactly 1 after the melting process
        iceMaterial.SetFloat("_Dissolve", 1f);
        isFullyMelted = true;  // Mark as fully melted
        meltCoroutine = null;  // Reset the coroutine reference
    }




    private IEnumerator RefreezeIce()
    {
        float initialDissolve = iceMaterial.GetFloat("_Dissolve");
        float elapsedTime = 0f;

        while (elapsedTime < freezeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newDissolve = Mathf.Lerp(initialDissolve, 0f, elapsedTime / freezeDuration);
            iceMaterial.SetFloat("_Dissolve", newDissolve);
            yield return null;
        }

        // Ensure the dissolve value is exactly 0 after refreezing
        iceMaterial.SetFloat("_Dissolve", 0f);
        isFullyMelted = false;  // Mark as not fully melted
        freezeCoroutine = null;  // Reset the coroutine reference
    }
}
