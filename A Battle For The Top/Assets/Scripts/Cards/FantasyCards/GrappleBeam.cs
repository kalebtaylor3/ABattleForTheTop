using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT;
using BFTT.Combat;
using BFTT.Components;
using BFTT.Abilities;
using BFTT.Controller;
using BFTT.IK;
using System;

public class GrappleBeam : AbstractCombat
{
    public static event Action<AbstractCombat> OnGrappleBreak;
    public float Durability = 5;
    RigidbodyMover _mover;
    private IKScheduler _ikScheduler;
    Vector3 _grapplePoint;
    bool _isGrappling = false;
    public float grappleSpeed = 10f; // Speed of the grappling movement
    public float grappleOffset = 0.5f; // Adjustable offset to prevent getting stuck
    public float maxGrappleDuration = 3f; // Maximum time allowed for grappling
    public Transform grappleSpawn;
    public Transform grappleHandReach;
    public Transform playerGrappleRotation;
    [SerializeField] private string grappleState = "Grapple";
    [SerializeField] private string downwardGrappleState = "Ladder";
    [SerializeField] private float grappleDelay = 1f; // Delay before pulling the player
    public GrappleRope rope;

    float originalAirControl;
    float grappleStartTime;

    public Transform leftFootTarget;
    public Transform rightFootTarget;

    public Transform leftFootTargetDown;
    public Transform rightFootTargetDown;
    public Transform leftHandTargetDown;

    public Transform leftHandTarget;

    private bool shouldMoveTowardsGrapplePoint = false;
    private Vector3 fixedIncrement;

    public Quaternion grappleRotation = new Quaternion(60, 0, 0, 1);

    public GameObject _cameraSpeedEffect;

    bool grappleUp = false;

    private void Awake()
    {
        _mover = GetComponent<RigidbodyMover>();
        _ikScheduler = GetComponent<IKScheduler>();
    }

    public override bool CombatReadyToRun()
    {
        // Check if the current card is this one and the action is set to use the card
        return _manager.currentCard == this && _action.UseCard;
    }

    public override void OnStartCombat()
    {
        Debug.Log("Calculating grapple point");

        if (_mover == null)
        {
            Debug.LogError("RigidbodyMover component not found on this GameObject.");
            return;
        }

        // Assuming you have a method to calculate the grapple point
        _grapplePoint = CalculateGrapplePoint();

        if (_grapplePoint != Vector3.zero)
        {
            _isGrappling = true;
            _manager._controller.canControl = false;
            _manager._controller.ResetActions();
            Debug.Log("Grapple point calculated: " + _grapplePoint);
            rope.lineRenderer.enabled = true; // Enable the LineRenderer
            Durability = Durability - 1;

            StartCoroutine(GrappleDelayCoroutine());
        }
        else
        {
            Debug.Log("Failed to calculate grapple point.");
            _manager._controller.canControl = true;
            _mover.SetIsGrappling(false);
            _isGrappling = false;
            StopCombat();
        }
    }

    public override void OnStopCombat()
    {
        Debug.Log("Stopping grapple");
        _mover.EnableCollision();
        _mover.EnableGravity();
        _manager._controller.canControl = true;
        _isGrappling = false;
        _mover.SetIsGrappling(false);
        _mover.SetRotation(new Quaternion(0, 0, 0, 1));
        rope.lineRenderer.enabled = false; // Disable the LineRenderer
        rope.lineRenderer.positionCount = 0;
        rope.spring.Reset();
        _manager._controller.canControl = true;
        _cameraSpeedEffect.SetActive(false);
        if (_ikScheduler != null)
        {
            _ikScheduler.StopIK(AvatarIKGoal.RightHand);
            _ikScheduler.StopIK(AvatarIKGoal.LeftFoot);
            _ikScheduler.StopIK(AvatarIKGoal.RightFoot);
            _ikScheduler.StopIK(AvatarIKGoal.LeftHand);
        }
    }

    public override void UpdateCombat()
    {
        if (!_isGrappling)
        {
            StopCombat();
            return;
        }
        HandleIK();
        if (shouldMoveTowardsGrapplePoint)
        {
            if(grappleUp)
                HandleFootIKUp();
            else
                HandleFootIKDown();
        }

        // Update the LineRenderer positions
        // rope.UpdateLineRenderer(grappleSpawn, _grapplePoint);
    }

    private IEnumerator GrappleDelayCoroutine()
    {
        // Show the grapple line for a duration before pulling the player
        float elapsedTime = 0f;

        while (elapsedTime < grappleDelay)
        {
            rope.UpdateLineRenderer(grappleSpawn, _grapplePoint);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _mover.DisableCollision();
        _mover.DisableGravity();
        _mover.StopMovement();
        _mover.SetIsGrappling(true);
        grappleStartTime = Time.time;
        _cameraSpeedEffect.SetActive(true);
        _abilityRunning.canMove = false;
        // Calculate the direction and rotation to the grapple point
        

        // Determine which animation state to use based on the angle
        if (_grapplePoint.y > transform.position.y)
        {
            GetComponent<Animator>().CrossFade(grappleState, 0.1f);
            grappleUp = true;
        }
        else
        {
            GetComponent<Animator>().CrossFade(downwardGrappleState, 0.1f);
            grappleUp = false;
        }

        shouldMoveTowardsGrapplePoint = true;

        Vector3 playerDirection = (_grapplePoint - _mover.transform.position).normalized;
        playerDirection.y = 0; // Flatten the direction on the Y-axis

        if (playerDirection != Vector3.zero) // Ensure we don't create an invalid rotation
        {
            // Calculate the Y-axis rotation
            Quaternion targetRotation = Quaternion.LookRotation(playerDirection);

            // Adjust the X-axis rotation (tilt)
            float tiltAngle;

            if (grappleUp)
                tiltAngle = -25; // Adjust this value to control the amount of tilt
            else
                tiltAngle = 30;
            Quaternion tiltRotation = Quaternion.Euler(tiltAngle, targetRotation.eulerAngles.y, targetRotation.eulerAngles.z);

            // Apply the rotation with tilt
            _mover.transform.rotation = tiltRotation;
        }

        // Calculate the fixed increment for consistent movement
        Vector3 direction = (_grapplePoint - transform.position).normalized;
        fixedIncrement = direction * grappleSpeed * Time.fixedDeltaTime;

        while (_isGrappling)
        {
            HandleIK();
            _mover.StopMovement();

            // Update the LineRenderer positions
            rope.UpdateLineRenderer(grappleSpawn, _grapplePoint);

            // Check if the player has reached the grapple point with an offset only in the y-axis
            Vector3 horizontalDistance = new Vector3(_grapplePoint.x - transform.position.x, 0, _grapplePoint.z - transform.position.z);
            float verticalDistance = Mathf.Abs(_grapplePoint.y - transform.position.y);

            if (horizontalDistance.magnitude < grappleOffset && verticalDistance < grappleOffset)
            {
                Debug.Log("Reached grapple point");
                GetComponent<Animator>().CrossFadeInFixedTime("Air.Falling", 0.1f);
                _manager._controller.canControl = true;
                _abilityRunning.canMove = true;
                if (Durability <= 0)
                {
                    abilityProp.SetActive(false);
                    OnGrappleBreak?.Invoke(this);
                    StopCombat();
                }
                StopCombat();
            }
            // Check if the maximum grapple duration has been exceeded
            else if (Time.time - grappleStartTime > maxGrappleDuration)
            {
                Debug.LogWarning("Grapple duration exceeded, stopping grapple");
                GetComponent<Animator>().CrossFadeInFixedTime("Air.Falling", 0.1f);
                _abilityRunning.canMove = true;
                _manager._controller.canControl = true;
                StopCombat();
            }

            yield return null;
        }

        shouldMoveTowardsGrapplePoint = false;
    }

    private void FixedUpdate()
    {
        if (shouldMoveTowardsGrapplePoint)
        {
            MoveTowardsGrapplePoint();
        }
    }

    private void MoveTowardsGrapplePoint()
    {
        Vector3 newPosition = transform.position + fixedIncrement;
        _mover.SetPosition(newPosition);
    }

    private Vector3 CalculateGrapplePoint()
    {
        // Calculate the grapple point based on the surface normal
        RaycastHit hit;
        Vector3 forward = Camera.main.transform.forward;
        Vector3 start = transform.position;
        start.y += 1.5f;

        Debug.DrawRay(start, forward * 10f, Color.red, 2f); // Draw the ray for debugging

        if (Physics.Raycast(start, forward, out hit, 20f))
        {
            Vector3 grapplePoint = hit.point;
            Debug.DrawLine(start, grapplePoint, Color.green, 2f); // Draw the line to the grapple point for debugging
            return grapplePoint;
        }

        return Vector3.zero;
    }

    private void HandleIK()
    {
        if (_grapplePoint != null && _ikScheduler != null)
        {
            // Right hand IK
            IKPass rightHandPass = new IKPass(grappleHandReach.position, grappleHandReach.rotation, AvatarIKGoal.RightHand, 1, 1);
            _ikScheduler.ApplyIK(rightHandPass);
        }
    }

    private void HandleFootIKUp()
    {
        if (_grapplePoint != null && _ikScheduler != null)
        {
            // Foot IK with flaring effect
            float flaringAmount = 0.1f; // Adjust as necessary
            float flaringSpeed = 10f; // Adjust as necessary

            Vector3 leftFootFlare = leftFootTarget.position + new Vector3(Mathf.Sin(Time.time * flaringSpeed) * flaringAmount, Mathf.Cos(Time.time * flaringSpeed) * flaringAmount, 0);
            Vector3 rightFootFlare = rightFootTarget.position + new Vector3(Mathf.Cos(Time.time * flaringSpeed) * flaringAmount, Mathf.Sin(Time.time * flaringSpeed) * flaringAmount, 0);
            Vector3 leftHandFlare = leftHandTarget.position + new Vector3(Mathf.Cos(Time.time * flaringSpeed) * flaringAmount, Mathf.Sin(Time.time * flaringSpeed) * flaringAmount, 0);

            IKPass leftFootPass = new IKPass(leftFootFlare, leftFootTarget.rotation, AvatarIKGoal.LeftFoot, 1, 1);
            IKPass rightFootPass = new IKPass(rightFootFlare, rightFootTarget.rotation, AvatarIKGoal.RightFoot, 1, 1);
            IKPass leftHandPass = new IKPass(leftHandFlare, leftHandTarget.rotation, AvatarIKGoal.LeftHand, 1, 1);

            _ikScheduler.ApplyIK(leftFootPass);
            _ikScheduler.ApplyIK(rightFootPass);
            _ikScheduler.ApplyIK(leftHandPass);
        }
    }

    private void HandleFootIKDown()
    {
        if (_grapplePoint != null && _ikScheduler != null)
        {
            // Foot IK with flaring effect
            float flaringAmount = 0.1f; // Adjust as necessary
            float flaringSpeed = 10f; // Adjust as necessary

            Vector3 leftFootFlare = leftFootTargetDown.position + new Vector3(Mathf.Sin(Time.time * flaringSpeed) * flaringAmount, Mathf.Cos(Time.time * flaringSpeed) * flaringAmount, 0);
            Vector3 rightFootFlare = rightFootTargetDown.position + new Vector3(Mathf.Cos(Time.time * flaringSpeed) * flaringAmount, Mathf.Sin(Time.time * flaringSpeed) * flaringAmount, 0);
            Vector3 leftHandFlare = leftHandTargetDown.position + new Vector3(Mathf.Cos(Time.time * flaringSpeed) * flaringAmount, Mathf.Sin(Time.time * flaringSpeed) * flaringAmount, 0);

            IKPass leftFootPass = new IKPass(leftFootFlare, leftFootTargetDown.rotation, AvatarIKGoal.LeftFoot, 1, 1);
            IKPass rightFootPass = new IKPass(rightFootFlare, rightFootTargetDown.rotation, AvatarIKGoal.RightFoot, 1, 1);
            IKPass leftHandPass = new IKPass(leftHandFlare, leftHandTargetDown.rotation, AvatarIKGoal.LeftHand, 1, 1);

            _ikScheduler.ApplyIK(leftFootPass);
            _ikScheduler.ApplyIK(rightFootPass);
            _ikScheduler.ApplyIK(leftHandPass);
        }
    }

    public override bool ReadyToExit()
    {
        if (!_isGrappling) return true;
        else return false;
    }
}
