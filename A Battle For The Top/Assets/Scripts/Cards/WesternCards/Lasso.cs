using System.Collections;
using UnityEngine;
using BFTT.Combat;
using BFTT.IK;

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

    // IK-related fields
    private IKScheduler _ikScheduler;
    public Transform startPosition;
    public Transform handRaisePosition;
    public Transform handBackPosition;
    public Transform handRestPosition;
    public Transform handPullPosition; // Add this field for the pull position
    public float windUpDelay = 0.3f; // Delay before launching the lasso

    private Animator _animator;

    private void Awake()
    {
        _ikScheduler = GetComponent<IKScheduler>();
        _animator = GetComponent<Animator>();
    }

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
            StartCoroutine(LassoHandIKSequence());
        }
        else
        {
            Debug.Log("Pulling Platform with Lasso");
            StartCoroutine(LassoHandIKSequence());
        }
    }

    private IEnumerator LassoHandIKSequence()
    {
        // Step 1: Raise the hand above the head
        //yield return StartCoroutine(SmoothIKTransition(handRaisePosition, 0.2f, startPosition)); // Smoothly transition to the raise position

        // Step 2: Move the hand back to wind up
        //yield return StartCoroutine(SmoothIKTransition(handBackPosition, windUpDelay, handRaisePosition)); // Smoothly transition to the back position



        // Step 3: Launch the lasso
        //yield return StartCoroutine(SmoothIKTransition(handRestPosition, 0.2f, handBackPosition)); // Smoothly transition to the rest position
        //ShootLasso(); // Launch the lasso after the hand has moved forward

        _animator.SetTrigger("Lasso");

        // Wait until the lasso action is complete
        yield return activeLassoRoutine;

        // Step 4: Ensure the IK stops after the action is complete
    }



    private IEnumerator SmoothIKTransition(Transform target, float duration, Transform _startPosition)
    {
        if (_ikScheduler == null || target == null) yield break;

        Vector3 startPosition = _startPosition.position;
        Quaternion startRotation = _startPosition.rotation;
        Vector3 endPosition = target.position;
        Quaternion endRotation = target.rotation;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            Vector3 interpolatedPosition = Vector3.Lerp(startPosition, endPosition, t);
            Quaternion interpolatedRotation = Quaternion.Slerp(startRotation, endRotation, t);

            IKPass rightHandPass = new IKPass(interpolatedPosition, interpolatedRotation, AvatarIKGoal.RightHand, 1f, 1f);
            _ikScheduler.ApplyIK(rightHandPass);

            yield return null;
        }

        // Ensure the final position and rotation are applied
        IKPass finalPass = new IKPass(endPosition, endRotation, AvatarIKGoal.RightHand, 1f, 1f);
        _ikScheduler.ApplyIK(finalPass);
    }


    private void ApplyIK(Transform target, float weight)
    {
        if (_ikScheduler != null && target != null)
        {
            IKPass rightHandPass = new IKPass(target.position, target.rotation, AvatarIKGoal.RightHand, weight, weight);
            _ikScheduler.ApplyIK(rightHandPass);
        }
    }

    public void ShootLasso()
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
        _animator.SetTrigger("Retract");

        // Step 4: Transition to pull position right before the actual pull starts
        yield return StartCoroutine(SmoothIKTransition(handPullPosition, 0.2f, handRestPosition));

        // Now we can start pulling the platform
        // Add the logic to start pulling the platform here, if necessary

        // Retract the lasso immediately after starting the pull
        activeLassoRoutine = StartCoroutine(RetractLasso());

        // Stop the IK after the retraction is done
        _ikScheduler.StopIK(AvatarIKGoal.RightHand);
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
        rope.lineRenderer.positionCount = 0;
        rope.spring.Reset();
        if (activeLassoRoutine != null)
        {
            StopCoroutine(activeLassoRoutine);
            activeLassoRoutine = null;
        }

        // Stop the IK when the combat stops
        _ikScheduler.StopIK(AvatarIKGoal.RightHand);

        isLassoActive = false;
        isRetracting = false;
    }


    public override bool ReadyToExit()
    {
        // The ability should not exit until the platform has been pulled and the lasso has retracted
        return !isLassoActive;
    }

    public bool GetIsRopeOut()
    {
        return lassoSwing;
    }
}