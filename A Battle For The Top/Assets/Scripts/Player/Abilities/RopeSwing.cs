using BFTT.Abilities;
using BFTT.Climbing;
using BFTT.Components;
using BFTT.IK;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class RopeSwing : AbstractAbility
{
    bool hasRope = false;
    private RigidbodyMover _mover;
    private ICapsule _capsule;

    private GameObject _currentRope;
    private GameObject _blockedLadder;

    [SerializeField] private float smoothnessTime = 0.12f;
    [SerializeField] private float swingForce = 10f; // Force applied for swinging
    [SerializeField] private float maxSwingSpeed = 5f; // Maximum speed for swinging
    [SerializeField] private float maxInputSwingAngle = 65f;
    [SerializeField] private float swingDamping = 0.985f;
    [SerializeField] private float hardSwingAngleLimit = 120f;
    [SerializeField] private float momentumTransferMultiplier = 0.85f;
    [SerializeField] private float ropeJumpLaunchForce = 11f;
    [SerializeField] private float ropeJumpUpwardForce = 4.5f;
    [SerializeField] private float ropeJumpLaunchSpeedMultiplier = 1.5f;
    [SerializeField] private float ropeJumpUpwardSpeedMultiplier = 0.45f;
    [SerializeField] private float maxReleaseSpeed = 14f;

    // Values to set position on the rope
    private Vector3 _startPosition, _targetPosition;
    private Quaternion _startRotation, _targetRotation;
    private float _step;
    private float _weight;
    [SerializeField] private float charOffsetY = 0.3f;
    [SerializeField] private float charOffsetX = 0.3f;
    [SerializeField] private float hangOffsetY = 0.25f;
    [SerializeField] private Transform grabReference;
    [SerializeField] private float overlapRange = 1f;
    [SerializeField] private LayerMask ropeMask;
    private Rigidbody _ropeRigidbody;
    private float _blockedTime;
    public string jumpBackState = "Climb.Jump From Wall";
    private float _targetDuration = 2f;
    private float _startTime;
    private IKScheduler _ikScheduler;
    private bool _isJumpReleasing;

    private Vector3 ropeForce;
    private Vector3 _ropeLocalCharacterOffset;

    [SerializeField] private string climbUpAnimState = "RopeIdle";

    [SerializeField] private ClimbStateContext _context;

    private void Awake()
    {
        _mover = GetComponent<RigidbodyMover>();
        _capsule = GetComponent<ICapsule>();
        _ikScheduler = GetComponent<IKScheduler>();
    }

    public override void OnStartAbility()
    {
        _isJumpReleasing = false;
        TransferPlayerMomentumToRope();
        _weight = 0;
        _step = 1 / smoothnessTime;
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _mover.DisableGravity();
        _mover.SetVelocity(Vector3.zero);
        _animator.SetFloat("HangWeight", 1);
        _animator.CrossFadeInFixedTime(climbUpAnimState, 0.1f);

        // Keep the character attached in rope-local space so the pose stays locked
        // to the grab point as the rope tilts and swings.
        _ropeLocalCharacterOffset = new Vector3(0f, -(charOffsetY + hangOffsetY), charOffsetX);
    }

    private void TransferPlayerMomentumToRope()
    {
        if (_ropeRigidbody == null) return;

        Vector3 incomingVelocity = _mover.GetVelocity();
        Vector3 swingPlaneVelocity = Vector3.ProjectOnPlane(incomingVelocity, _ropeRigidbody.transform.right);

        if (swingPlaneVelocity.sqrMagnitude <= 0.0001f)
            return;

        _ropeRigidbody.AddForce(swingPlaneVelocity * momentumTransferMultiplier, ForceMode.VelocityChange);
    }

    public override void OnStopAbility()
    {
        _isJumpReleasing = false;
        if (_ikScheduler != null)
        {
            _ikScheduler.StopIK(AvatarIKGoal.RightHand);
            _ikScheduler.StopIK(AvatarIKGoal.LeftHand);
        }
        base.OnStopAbility(); 
    }

    private bool FoundRope()
    {
        var overlaps = Physics.OverlapSphere(grabReference.position, overlapRange);

        // Loop through all overlaps
        foreach (var coll in overlaps)
        {
            if (coll.gameObject.tag == "Rope")
            {
                if (_currentRope == _blockedLadder && Time.time - _blockedTime < 2f)
                    continue;

                if (CanGrab(coll.gameObject))
                {
                    _currentRope = coll.gameObject;
                    _ropeRigidbody = _currentRope.GetComponent<Rope>().ropeRigidbody;
                    _currentRope = _currentRope.GetComponent<Rope>().attachPoint.gameObject;
                    hasRope = true;
                    return true;
                }
            }
        }

        return false;
    }

    public bool CanGrab(GameObject rope)
    {
        // Can't grab if character is not looking on ladder
        if (Vector3.Dot(transform.forward, rope.transform.forward) < -0.1f)
        {
            Debug.Log("Cant grab rope from this way");
            return false;
        }

        return true;
    }

    private void AttachToRope()
    {
        _weight = Mathf.MoveTowards(_weight, 1f, _step * Time.deltaTime);
        _mover.SetPosition(Vector3.Lerp(_startPosition, _targetPosition, _weight));
        transform.rotation = Quaternion.Lerp(_startRotation, _targetRotation, _weight);
    }

    public override bool ReadyToRun()
    {
        return FoundRope();
    }


    public override void UpdateAbility()
    {
        if (_isJumpReleasing)
            return;

        Debug.Log("onRope");
        _targetPosition = GetCharPosition();
        _targetRotation = GetCharRotation();
        _mover.SetVelocity(Vector3.zero);
        _mover.SetPosition(_targetPosition);
        _mover.SetRotation(_targetRotation);
        HandleIK();

        HandleSwingInput();

        if (_action.drop)
        {
            _mover.EnableGravity();
            StopAbility();
            BlockRope();
        }

        if (_action.jump)
        {
            _isJumpReleasing = true;
            BlockRope();
            _mover.EnableGravity();
            Vector3 ropeVelocity = _ropeRigidbody.linearVelocity;
            float releaseSpeed = Mathf.Min(ropeVelocity.magnitude, maxReleaseSpeed);
            Vector3 releaseDirection = ropeVelocity.sqrMagnitude > 0.001f
                ? ropeVelocity.normalized
                : _currentRope.transform.forward;
            float scaledLaunchForce = ropeJumpLaunchForce + releaseSpeed * ropeJumpLaunchSpeedMultiplier;
            float scaledUpwardForce = ropeJumpUpwardForce + releaseSpeed * ropeJumpUpwardSpeedMultiplier;

            _mover.SetVelocity(ropeVelocity);
            _mover.GetComponent<Rigidbody>().AddForce(
                releaseDirection * scaledLaunchForce + Vector3.up * scaledUpwardForce,
                ForceMode.VelocityChange);
            _animator.CrossFadeInFixedTime(jumpBackState, 0.1f);
            StartCoroutine(WaitJumpBackAnimation(0.62f, _context));
            return;
        }

    }

    private IEnumerator WaitJumpBackAnimation(float targetNormalizedtime, ClimbStateContext context)
    {
        float normalizedTime = 0;
        while (Mathf.Repeat(normalizedTime, 1) < targetNormalizedtime)
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);

            if (state.IsName(jumpBackState))
                normalizedTime = state.normalizedTime;

            // Constantly update start time to avoid call this method twice
            _startTime = Time.time;
            yield return null;
        }

        StopAbility();
    }

    private void BlockRope()
    {
        _blockedLadder = _currentRope;
        _blockedTime = Time.time;
    }

    private void HandleSwingInput()
    {
        if (_ropeRigidbody == null) return;

        float verticalInput = _action.move.y; // Get vertical input (W/S or Up Arrow/Down Arrow)
        float currentSwingAngle = Vector3.Angle(Vector3.down, _ropeRigidbody.transform.up);
        float inputAngleFactor = 1f - Mathf.Clamp01(currentSwingAngle / maxInputSwingAngle);
        Vector3 swingDirection = Vector3.ProjectOnPlane(_ropeRigidbody.linearVelocity, _ropeRigidbody.transform.right);

        // If the rope is already too high in the arc, strip any velocity that would carry it
        // farther upward so the player can't brute-force over the top.
        if (currentSwingAngle >= hardSwingAngleLimit && swingDirection.y > 0f)
        {
            Vector3 clampedVelocity = _ropeRigidbody.linearVelocity;
            clampedVelocity.y = Mathf.Min(clampedVelocity.y, 0f);
            _ropeRigidbody.linearVelocity = clampedVelocity;
            inputAngleFactor = 0f;
        }

        // Calculate the force to apply in local space
        ropeForce = new Vector3(0, 0, verticalInput * swingForce * inputAngleFactor);
        Vector3 localRopeForce = _ropeRigidbody.transform.TransformDirection(ropeForce);

        // Apply the force to the Rigidbody of the hinge joint in local space
        if (_ropeRigidbody.linearVelocity.magnitude < maxSwingSpeed && Mathf.Abs(verticalInput) > 0.01f)
        {
            _ropeRigidbody.AddForce(localRopeForce);
        }

        _ropeRigidbody.linearVelocity *= swingDamping;
    }

    public Vector3 GetCharPosition()
    {
        return _currentRope.transform.TransformPoint(_ropeLocalCharacterOffset);
    }

    public Quaternion GetCharRotation()
    {
        return Quaternion.LookRotation(_currentRope.transform.forward);
    }

    private void HandleIK()
    {
        if (_currentRope != null && _ikScheduler != null)
        {
            // Right hand IK
            IKPass rightHandPass = new IKPass(_currentRope.transform.position, _currentRope.transform.rotation, AvatarIKGoal.RightHand, 1, 1);
            _ikScheduler.ApplyIK(rightHandPass);

            IKPass leftHandPass = new IKPass(_currentRope.transform.position, _currentRope.transform.rotation, AvatarIKGoal.LeftHand, 1, 1);
            _ikScheduler.ApplyIK(leftHandPass);
        }
    }
}
