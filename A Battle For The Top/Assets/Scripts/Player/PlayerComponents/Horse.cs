using BFTT;
using BFTT.Abilities;
using BFTT.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Horse : MonoBehaviour
{

    public Transform saddlePosition;
    public Transform leftHandPosition;
    public Transform rightHandPosition;
    public Transform leftFootPosition;
    public Transform rightFootPosition;

    public HorseMover _mover;

    [HideInInspector] public bool _beingRode;

    public AbilityScheduler _scheduler;



    bool happenOnce = false;
    private float jumpHeight = 2;
    private float _startSpeed;
    private float speedOnAir = 6f;
    private Vector2 _startInput;

    // Start is called before the first frame update
    void Start()
    {
        _mover.enabled = false;
    }

    private void OnEnable()
    {
        _mover.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(_beingRode)
        {

            float targetSpeed = 0;
            targetSpeed = _scheduler.characterActions.walk ? 7 : 2.5f;
            _mover.enabled = true;
            _mover.Move(_scheduler.characterActions.move, targetSpeed);
            Debug.Log(_scheduler.characterActions.move);
            happenOnce = false;

            if (_scheduler.characterActions.jump)
                PerformJump();
        }
        else
        {
            if (!happenOnce)
            {
                _mover.StopMovement();
                _mover.enabled = false;
                happenOnce = true;
            }
        }
    }

    private void PerformJump()
    {

        _startSpeed = Vector3.Scale(_mover.GetVelocity(), new Vector3(1, 0, 1)).magnitude;
        _startInput.x = Vector3.Dot(Camera.main.transform.right, transform.forward);
        _startInput.y = Vector3.Dot(Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)), transform.forward);

        Vector3 velocity = _mover.GetVelocity();
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * _mover.GetGravity());

        _mover.SetVelocity(velocity);
        _mover._animator.CrossFadeInFixedTime("Jump", 0.1f);
        _startSpeed = speedOnAir;

        if (_startInput.magnitude > 0.1f)
            _startInput.Normalize();

        //if (_audioPlayer)
        //    _audioPlayer.PlayVoice(jumpVoice);

        //if (_audioPlayer)
        //    _audioPlayer.PlayEffect(jumpEffort);
    }
}
