using BFTT.Abilities;
using BFTT.Components;
using BFTT.IK;
using BFTT.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RideHorse : AbstractAbility
{
    [HideInInspector] public bool ridingHorse = false;
    [SerializeField] private string rideState = "Ride";
    [SerializeField] private float detectionRange = 5f; // Range to detect the horse

    private IMover _mover;
    private ICapsule _capsule;
    private IKScheduler _ikScheduler;
    private Transform saddleTransform;
    private Horse horse;
    public DisplayMessage simpleMessageDisplay;

    public AbstractCombat horseCard;

    bool showMessage = false;

    private void Awake()
    {
        _mover = GetComponent<IMover>();
        _capsule = GetComponent<ICapsule>();
        _ikScheduler = GetComponent<IKScheduler>();
    }

    public override void OnStartAbility()
    {
        if (saddleTransform != null)
        {
            _mover.StopRootMotion();
            _mover.SetVelocity(Vector3.zero);
            _mover.DisableGravity();
            _animator.CrossFadeInFixedTime(rideState, 0.1f);
            _mover.SetPosition(saddleTransform.position);
            _mover.SetRotation(saddleTransform.rotation);
            horse._mover._animator.CrossFadeInFixedTime("Grounded", 0.1f);
        }
    }

    public override bool ReadyToRun()
    {
        // Check if there is a horse within range
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
        bool horseInRange = false;
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Horse"))
            {
                horseInRange = true;
                horse = collider.GetComponent<Horse>();
                if (horseCard.CombatReadyToRun())
                {
                    if (_action.interact)
                    {
                        saddleTransform = horse.saddlePosition;
                        horse._beingRode = true;
                        ridingHorse = true;
                        simpleMessageDisplay.SetShowMessage(false, "Horse Card", "Must be equipped to ride!"); // Hide the message
                        return true;
                    }
                }
                else
                {
                    simpleMessageDisplay.SetShowMessage(true, "Horse Card", "Must be equipped to ride!"); // Show the message
                }
            }
        }

        if (!horseInRange || (horseInRange && horseCard.CombatReadyToRun()))
        {
            simpleMessageDisplay.SetShowMessage(false, "Horse Card", "Must be equipped to ride!"); // Hide the message if not in range or combat is ready
        }

        ridingHorse = false;
        saddleTransform = null;
        return false;
    }

    public override void UpdateAbility()
    {
        Debug.Log("Riding Horse");
        HandleIK();
        FootIK();
        _mover.SetPosition(saddleTransform.position);
        _mover.SetRotation(saddleTransform.rotation);
        // If drop action is performed, stop riding the horse
        if (_action.drop)
        {
            ridingHorse = false;
            if (_ikScheduler != null)
            {
                _ikScheduler.StopIK(AvatarIKGoal.LeftHand);
                _ikScheduler.StopIK(AvatarIKGoal.RightHand);
                _ikScheduler.StopIK(AvatarIKGoal.LeftFoot);
                _ikScheduler.StopIK(AvatarIKGoal.RightFoot);
            }
            horse._mover._animator.CrossFadeInFixedTime("Idle", 0.1f);
            horse._beingRode = false;
            _mover.EnableGravity();
            StopAbility();
        }
    }

    private void HandleIK()
    {
        if (horse != null && _ikScheduler != null)
        {
            // left hand
            Transform lhEffector = horse.leftHandPosition;
            if (lhEffector != null)
            {
                IKPass leftHandPass = new IKPass(lhEffector.position,
                    lhEffector.rotation,
                    AvatarIKGoal.LeftHand,
                    1, 1);

                _ikScheduler.ApplyIK(leftHandPass);
            }

            // right hand
            Transform rhEffector = horse.rightHandPosition;
            if (rhEffector != null)
            {
                IKPass rightHandPass = new IKPass(rhEffector.position,
                    rhEffector.rotation,
                    AvatarIKGoal.RightHand,
                    1, 1);

                _ikScheduler.ApplyIK(rightHandPass);
            }

        }
    }

    private void FootIK()
    {
        if (horse != null && _ikScheduler != null)
        {
            // left foot
            Transform lfEffector = horse.leftFootPosition;
            if (lfEffector != null)
            {
                IKPass leftFootPass = new IKPass(lfEffector.position,
                    lfEffector.rotation,
                    AvatarIKGoal.LeftFoot,
                    1, 1);

                _ikScheduler.ApplyIK(leftFootPass);
            }

            // right foot
            Transform rfEffector = horse.rightFootPosition;
            if (rfEffector != null)
            {
                IKPass rightFootPass = new IKPass(rfEffector.position,
                    rfEffector.rotation,
                    AvatarIKGoal.RightFoot,
                    1, 1);

                _ikScheduler.ApplyIK(rightFootPass);
            }

        }
    }
}
