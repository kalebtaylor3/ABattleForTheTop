using System.Collections;
using UnityEngine;
using BFTT.Combat;

public class Lasso : AbstractCombat
{
    private bool lassoSwing = false;
    public Transform lassoSpawn; // Where the lasso is shot from
    public float lassoSpeed = 20f; // Speed of the lasso shot
    public float maxLassoDistance = 30f; // Maximum distance the lasso can travel
    public LayerMask lassoLayerMask; // Layer mask for detecting eligible platforms
    public LassoRope rope; // Reference to the LassoRope script
    public float retractDuration;

    private Vector3 lassoPoint; // The point where the lasso hits or attaches
    private bool isLassoActive = false;
    private Transform attachedObject; // The platform or object we're pulling or swinging from

    private Coroutine activeLassoRoutine; // Store the active lasso routine
    private bool isRetracting = false; // Track whether the lasso is retracting

    public override bool CombatReadyToRun()
    {
        if (_manager.currentCard == this && _action.UseCard)
        {
            lassoSwing = false;
            return true;
        }

        if (_manager.currentCard == this && _action.zoom && _action.UseCard)
        {
            lassoSwing = true;
            return true;
        }
        return false;
    }

    public override void OnStartCombat()
    {
        if (lassoSwing)
        {
            Debug.Log("Swinging from Lasso");
            ShootLasso();
        }
        else
        {
            Debug.Log("Pulling Platform with Lasso");
            ShootLasso();
        }
    }

    private void ShootLasso()
    {
        // Cancel any active retraction or previous lasso routine
        if (activeLassoRoutine != null)
        {
            StopCoroutine(activeLassoRoutine);
            activeLassoRoutine = null;
        }

        // Reset the lasso rope before shooting again
        rope.lineRenderer.positionCount = 0;
        rope.spring.Reset();
        isRetracting = false; // Reset retracting flag

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, maxLassoDistance, lassoLayerMask))
        {
            lassoPoint = hit.point;
            attachedObject = hit.transform; // The object the lasso hit
            isLassoActive = true;

            activeLassoRoutine = StartCoroutine(ShootAndPullLasso());
        }
        else
        {
            Debug.Log("Lasso missed");
            StopCombat();
        }
    }

    private IEnumerator ShootAndPullLasso()
    {
        rope.EnableLasso();
        float shootDuration = Vector3.Distance(lassoSpawn.position, lassoPoint) / lassoSpeed;

        float elapsedTime = 0f;
        while (elapsedTime < shootDuration)
        {
            Vector3 currentLassoPosition = Vector3.Lerp(lassoSpawn.position, lassoPoint, elapsedTime / shootDuration);
            rope.UpdateLineRenderer(lassoSpawn, currentLassoPosition);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the lasso reaches the target
        rope.UpdateLineRenderer(lassoSpawn, lassoPoint);

        // Wait for a brief moment while the lasso is attached
        yield return new WaitForSeconds(0.5f);

        // Start pulling the platform briefly
        

        // Retract the lasso immediately after starting the pull
        activeLassoRoutine = StartCoroutine(RetractLasso());
    }

    private IEnumerator RetractLasso()
    {
        isRetracting = true;
        float elapsedTime = 0f;

        while (elapsedTime < retractDuration)
        {
            Vector3 retractPosition = Vector3.Lerp(lassoPoint, lassoSpawn.position, elapsedTime / retractDuration);
            rope.UpdateLineRenderer(lassoSpawn, retractPosition); // Update the lasso to simulate retraction
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        rope.DisableLasso();
        rope.lineRenderer.positionCount = 0;
        rope.spring.Reset();
        isLassoActive = false;
        isRetracting = false;
        StopCombat();
    }

    public override void UpdateCombat()
    {
        // If the lasso is active, keep updating the rope's position
        if (isLassoActive && !isRetracting)
        {
            rope.UpdateLineRenderer(lassoSpawn, lassoPoint);
        }

        // Handle swinging update
        if (isLassoActive && lassoSwing)
        {
            if (!_action.UseCard) // Stop swinging and retract when the action is released
            {
                StopSwinging();
            }
        }
    }

    private void StopSwinging()
    {
        Debug.Log("Stopping swinging");
        if (activeLassoRoutine != null)
        {
            StopCoroutine(activeLassoRoutine);
        }
        activeLassoRoutine = StartCoroutine(RetractLasso());
    }

    public override void OnStopCombat()
    {
        if (!isLassoActive)
        {
            if (activeLassoRoutine != null)
            {
                StopCoroutine(activeLassoRoutine);
            }
            activeLassoRoutine = StartCoroutine(RetractLasso());
        }
    }

    public override bool ReadyToExit()
    {
        // The ability should not exit until the platform has been pulled and the lasso has retracted
        return !isLassoActive;
    }

    public bool GetIsSwining()
    {
        return lassoSwing;
    }
}
