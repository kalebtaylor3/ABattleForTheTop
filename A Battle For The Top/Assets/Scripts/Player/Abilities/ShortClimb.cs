using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Components;
using BFTT.Debugging;
using BFTT.Controller;

namespace BFTT.Abilities
{
    public class ShortClimb : AbstractAbility
    {
        [SerializeField] private LayerMask shortClimbMask;
        [SerializeField] private float overlapRadius = 0.75f;
        [SerializeField] private float capsuleCastRadius = 0.2f;
        [SerializeField] private float capsuleCastHeight = 1f;
        [SerializeField] private float minClimbHeight = 0.5f;
        [SerializeField] private float maxClimbHeight = 1.5f;
        [Header("Animation")]
        [SerializeField] private string shortClimbAnimState = "Short Climb";

        private IMover _mover;
        private ICapsule _capsule;
        private CastDebug _debug;

        private RaycastHit _targetHit;
        private bool _hasMatchTarget;

        [HideInInspector] public CharacterAudioPlayer _audioPlayer;

        public AudioClip climbEffect;

        bool movingPlatform = false;
        Transform movingPlatformTransform;
        private PlayerController _controller;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();
            _audioPlayer = GetComponent<CharacterAudioPlayer>();
            _debug = GetComponent<CastDebug>();
            _controller = GetComponent<PlayerController>();
        }

        public override bool ReadyToRun()
        {
            return !_mover.IsGrounded() && HasShortClimb();
        }

        public override void OnStartAbility()
        {
            _capsule.DisableCollision();
            _mover.DisableGravity();
            _mover.ApplyRootMotion(Vector3.one);
            _mover.StopMovement();
            _audioPlayer.PlayEffect(climbEffect);

            _animator.CrossFadeInFixedTime(shortClimbAnimState, 0.1f);
            _hasMatchTarget = false;
        }


        public override void UpdateAbility()
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);

            if (_animator.IsInTransition(0) || !state.IsName(shortClimbAnimState)) return;

            var normalizedTime = Mathf.Repeat(state.normalizedTime, 1f);
            if (!_animator.isMatchingTarget && !_hasMatchTarget)
            {

                if (movingPlatform)
                    transform.SetParent(movingPlatformTransform);
                else
                    transform.SetParent(_controller.GetOriginalParent());

                // calculate target position
                Vector3 targetPosition = _targetHit.point - _targetHit.normal * _capsule.GetCapsuleRadius() * 0.5f;
                _animator.MatchTarget(targetPosition, Quaternion.identity, AvatarTarget.Root,
                    new MatchTargetWeightMask(Vector3.one, 0f), 0.15f, 0.42f);

                _hasMatchTarget = true;
                if (_debug)
                {
                    _debug.DrawCapsule(targetPosition + Vector3.up * _capsule.GetCapsuleRadius(),
                        targetPosition + Vector3.up * (_capsule.GetCapsuleHeight() - _capsule.GetCapsuleRadius()),
                        _capsule.GetCapsuleRadius(), Color.yellow, 3f);

                    _debug.DrawLabel("Short Climb: target position", _targetHit.point + Vector3.up, Color.yellow, 3f);
                }
            }

            if (normalizedTime > 0.95f)
                StopAbility();
        }

        public override void OnStopAbility()
        {
            _capsule.EnableCollision();
            _mover.EnableGravity();
            _mover.StopRootMotion();
            _mover.StopMovement();
        }

        private bool HasShortClimb()
        {
            Vector3 overlapCenter = transform.position + Vector3.up * overlapRadius;

            if(Physics.OverlapSphere(overlapCenter, overlapRadius, shortClimbMask, QueryTriggerInteraction.Collide).Length > 0)
            { // found some short climb object

                // capsule cast points
                Vector3 p1 = transform.position + Vector3.up * (minClimbHeight + capsuleCastRadius);
                Vector3 p2 = transform.position + Vector3.up * (capsuleCastHeight - capsuleCastRadius);
                Vector3 castDirection = transform.forward;

                if(Physics.CapsuleCast(p1,p2, capsuleCastRadius, castDirection, out RaycastHit forwardHit,
                    overlapRadius, shortClimbMask, QueryTriggerInteraction.Collide))
                {

                    if (forwardHit.collider.CompareTag("MovingPlatform"))
                    {
                        movingPlatform = true;
                        movingPlatformTransform = forwardHit.collider.transform;
                    }
                    else
                    {
                        movingPlatform = false;
                    }

                    Vector3 sphereStart = forwardHit.point;
                    sphereStart.y = transform.position.y + maxClimbHeight + capsuleCastRadius;

                    // check top
                    if(Physics.SphereCast(sphereStart, capsuleCastRadius, Vector3.down, out RaycastHit topHit, maxClimbHeight - minClimbHeight, 
                        shortClimbMask, QueryTriggerInteraction.Collide))
                    {
                        _targetHit = topHit;
                        _targetHit.normal = Vector3.Scale(forwardHit.normal, new Vector3(1,0,1)).normalized;

                        if (_debug)
                            _debug.DrawSphere(_targetHit.point, 0.1f, Color.red, 3f);
                        
                        return true;
                    }
                }

            }

            return false;
        }
    }
}