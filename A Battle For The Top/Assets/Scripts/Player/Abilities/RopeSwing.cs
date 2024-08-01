using BFTT.Abilities;
using BFTT.Climbing;
using BFTT.Components;
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

    // Values to set position on the rope
    private Vector3 _startPosition, _targetPosition;
    private Quaternion _startRotation, _targetRotation;
    private float _step;
    private float _weight;
    [SerializeField] private float charOffsetY = 0.3f;
    [SerializeField] private float charOffsetX = 0.3f;
    [SerializeField] private Transform grabReference;
    [SerializeField] private float overlapRange = 1f;
    [SerializeField] private LayerMask ropeMask;
    private Rigidbody _ropeRigidbody;
    private float _blockedTime;
    public string jumpBackState = "Climb.Jump From Wall";
    private float _targetDuration = 2f;
    private float _startTime;

    private Vector3 ropeForce;

    [SerializeField] private string climbUpAnimState = "Climb.Idle";

    [SerializeField] private ClimbStateContext _context;

    private void Awake()
    {
        _mover = GetComponent<RigidbodyMover>();
        _capsule = GetComponent<ICapsule>();
    }

    public override void OnStartAbility()
    {
        _ropeRigidbody.AddForce(new Vector3(0, 0, _mover.GetVelocity().z));
        _weight = 0;
        _step = 1 / smoothnessTime;
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _mover.DisableGravity();
        _animator.SetFloat("HangWeight", 1);
        _animator.CrossFadeInFixedTime(climbUpAnimState, 0.1f);
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
        if (Vector3.Dot(transform.forward, rope.transform.forward) < -0.1f) return false;

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
        Debug.Log("onRope");
        _targetPosition = GetCharPosition();
        _targetRotation = GetCharRotation();
        _mover.SetPosition(_targetPosition);
        transform.rotation = _targetRotation;

        HandleSwingInput();

        if (_action.drop)
        {
            _mover.EnableGravity();
            StopAbility();
            BlockRope();
        }

        if (_action.jump)
        {
            BlockRope();
            _mover.EnableGravity();
            _mover.SetVelocity(_ropeRigidbody.velocity);
            _mover.GetComponent<Rigidbody>().AddForce(_ropeRigidbody.velocity, ForceMode.VelocityChange);
            _animator.CrossFadeInFixedTime(jumpBackState, 0.1f);
            StartCoroutine(WaitJumpBackAnimation(0.62f, _context));
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

        _targetDuration = 2f;
        transform.rotation = Quaternion.LookRotation(ropeForce);
        _mover.SetVelocity(_ropeRigidbody.velocity * 2);
        _mover.GetComponent<Rigidbody>().AddForce(_ropeRigidbody.velocity, ForceMode.VelocityChange);

        _startTime = Time.time;
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

        float horizontalInput = _action.move.x; // Get horizontal input (A/D or Left Arrow/Right Arrow)
        float verticalInput = _action.move.y; // Get vertical input (W/S or Up Arrow/Down Arrow)

        // Calculate the force to apply in local space
        ropeForce = new Vector3(0, 0, verticalInput * swingForce);
        Vector3 localRopeForce = _ropeRigidbody.transform.TransformDirection(ropeForce);

        // Apply the force to the Rigidbody of the hinge joint in local space
        if (_ropeRigidbody.velocity.magnitude < maxSwingSpeed)
        {
            _ropeRigidbody.AddForce(localRopeForce);
        }
    }

    public Vector3 GetCharPosition()
    {
        Vector3 position = _currentRope.transform.position + _currentRope.transform.forward * charOffsetX;
        position.y = _currentRope.transform.position.y - charOffsetY;

        return position;
    }

    public Quaternion GetCharRotation()
    {
        return Quaternion.LookRotation(_currentRope.transform.forward);
    }
}
