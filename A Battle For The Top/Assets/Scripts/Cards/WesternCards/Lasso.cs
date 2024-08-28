using System.Collections;
using System.Collections.Generic;
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

    private Vector3 lassoPoint; // The point where the lasso hits or attaches
    private bool isLassoActive = false;
    private Transform attachedObject; // The platform or object we're pulling or swinging from

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
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, maxLassoDistance, lassoLayerMask))
        {
            lassoPoint = hit.point;
            attachedObject = hit.transform; // The object the lasso hit
            isLassoActive = true;

            rope.EnableLasso(); // Enable the LineRenderer for the rope
            rope.UpdateLineRenderer(lassoSpawn, lassoPoint); // Initial rope update

            if (lassoSwing)
            {
                StartSwinging();
            }
            else
            {
                StartCoroutine(PullPlatform());
            }
        }
        else
        {
            Debug.Log("Lasso missed");
            StopCombat();
        }
    }

    private void StartSwinging()
    {
        Debug.Log("Started swinging from point: " + lassoPoint);

        // You can integrate swing mechanics here or call an existing function
    }

    private IEnumerator PullPlatform()
    {
        if (attachedObject == null)
        {
            Debug.LogWarning("No object to pull. Stopping combat.");
            StopCombat();
            yield break;
        }

        Vector3 originalPosition = attachedObject.position;
        Vector3 targetPosition = lassoSpawn.position;

        float pullDuration = 1.5f; // Time taken to pull the platform
        float elapsedTime = 0f;

        while (elapsedTime < pullDuration)
        {
            if (attachedObject != null)
            {
                attachedObject.position = Vector3.Lerp(originalPosition, targetPosition, elapsedTime / pullDuration);
                rope.UpdateLineRenderer(lassoSpawn, lassoPoint); // Update rope position during pull
            }
            else
            {
                Debug.LogWarning("Attached object became null during the pull.");
                break;
            }
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        StartCoroutine(RetractLasso());
    }

    private IEnumerator RetractLasso()
    {
        float retractDuration = 0.5f; // Time to retract
        float elapsedTime = 0f;

        while (elapsedTime < retractDuration)
        {
            rope.UpdateLineRenderer(lassoSpawn, lassoSpawn.position); // Update the lasso to simulate retraction
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        rope.DisableLasso();
        rope.lineRenderer.positionCount = 0;
        rope.spring.Reset();
        isLassoActive = false;
        StopCombat();
    }

    public override void UpdateCombat()
    {
        if (!_action.UseCard && !isLassoActive)
        {
            StopCombat();
            return;
        }

        if (isLassoActive && lassoSwing)
        {
            rope.UpdateLineRenderer(lassoSpawn, lassoPoint); // Handle swinging update

            if (!_action.UseCard) // Stop swinging and retract when the action is released
            {
                StopSwinging();
            }
        }
    }

    private void StopSwinging()
    {
        Debug.Log("Stopping swinging");
        StartCoroutine(RetractLasso());
    }

    public override void OnStopCombat()
    {
        if (!isLassoActive)
        {
            StartCoroutine(RetractLasso());
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
