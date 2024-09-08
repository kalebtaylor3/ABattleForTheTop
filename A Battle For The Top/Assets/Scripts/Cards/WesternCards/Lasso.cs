using System.Collections;
using UnityEngine;
using BFTT.Combat;
using BFTT.IK;
using BFTT.Components;
using BFTT.Abilities;

public class Lasso : AbstractCombat
{
    private bool lassoSwing = false;
    private bool swingingActive = false; // Toggle for starting/stopping swing
    public Transform lassoSpawn; // Where the lasso is shot from
    public float lassoSpeed = 20f; // Speed of the lasso shot
    public float maxLassoDistance = 30f; // Maximum distance the lasso can travel
    public LayerMask lassoLayerMask; // Layer mask for detecting eligible platforms
    public LassoRope rope; // Reference to the LassoRope script
    public float retractDuration;
    public float swingAmplitude = 3f; // Amplitude of the swinging motion

    private Vector3 lassoPoint; // The point where the lasso hits or attaches
    private bool isLassoActive = false;
    private Transform attachedObject; // The platform or object we're pulling or swinging from

    private Coroutine activeLassoRoutine; // Store the active lasso routine
    private bool isRetracting = false; // Track whether the lasso is retracting
    private bool isSwinging = false; // Track if the player is swinging

    // IK-related fields
    private IKScheduler _ikScheduler;
    private RigidbodyMover _mover;
    public Transform startPosition;
    public Transform handRaisePosition;
    public Transform handBackPosition;
    public Transform handRestPosition;
    public Transform handPullPosition; // Add this field for the pull position
    public float windUpDelay = 0.3f; // Delay before launching the lasso
    private Strafe _zoomAbility;

    private Animator _animator;
    private Rigidbody _rigidbody; // For handling player movement

    private void Awake()
    {
        _ikScheduler = GetComponent<IKScheduler>();
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>(); // Assuming the player has a Rigidbody
        _mover = GetComponent<RigidbodyMover>();
        _zoomAbility = GetComponent<Strafe>();
    }

    public override bool CombatReadyToRun()
    {
        if (_manager.currentCard == this && _action.UseCard && !_action.zoom)
        {
            lassoSwing = false;
            return true;
        }

        if (_manager.currentCard == this && _action.zoom && _action.UseCard)
        {
            lassoSwing = true;
            _zoomAbility.canZoom = false;
            _ikScheduler.StopIK(AvatarIKGoal.RightHand);
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
        _animator.SetTrigger("Lasso");

        // Wait until the lasso action is complete
        yield return activeLassoRoutine;

        // Ensure the IK stops after the action is complete
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

            if (lassoSwing)
            {
                activeLassoRoutine = StartCoroutine(AttachForSwing()); // Attach for swinging
            }
            else
            {
                activeLassoRoutine = StartCoroutine(ShootAndPullLasso()); // Start pulling
            }
        }
        else
        {
            Debug.Log("Lasso missed");
            // Stop the IK when the combat stops
            _ikScheduler.StopIK(AvatarIKGoal.RightHand);
            _animator.SetTrigger("Retract");
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

    private IEnumerator AttachForSwing()
    {
        // Attach the lasso to the target but don't apply immediate force
        rope.EnableLasso();
        isSwinging = true;

        Debug.Log("Lasso attached for swinging");

        while (isSwinging)
        {
            // If the player is grounded, don't apply force (let them hang)
            if (!_mover.Grounded)
            {
                // If the player leaves the ground, allow swinging motion
                Vector3 swingDirection = new Vector3(Mathf.Sin(Time.time), Mathf.Cos(Time.time), 0) * swingAmplitude;
                Vector3 swingPosition = lassoPoint + swingDirection;

                transform.position = Vector3.Lerp(transform.position, swingPosition, Time.deltaTime);
            }

            rope.UpdateLineRenderer(lassoSpawn, lassoPoint);
            yield return null;
        }
    }


    private void DetachSwinging()
    {
        if (!isSwinging) return;

        isSwinging = false;
        lassoSwing = false;
        Debug.Log("Stopped swinging");

        if (activeLassoRoutine != null)
        {
            StopCoroutine(activeLassoRoutine);
        }

        _animator.SetTrigger("Retract");
        // Start retraction immediately after stopping swing
        activeLassoRoutine = StartCoroutine(RetractLasso());
        _zoomAbility.canZoom = true;
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

        if (lassoSwing && !_action.zoom)
        {
            Debug.Log("Player stopped aiming, detaching lasso");
            DetachSwinging(); // Stop swinging and retract when the player stops aiming
            _zoomAbility.canZoom = false;
        }
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
        isSwinging = false;
        _zoomAbility.canZoom = true;
    }

    public override bool ReadyToExit()
    {
        return !isLassoActive;
    }

    public bool GetIsRopeOut()
    {
        return lassoSwing;
    }
}
